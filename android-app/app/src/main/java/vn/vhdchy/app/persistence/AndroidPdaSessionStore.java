package vn.vhdchy.app.persistence;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.nio.charset.StandardCharsets;
import java.security.Key;
import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

import vn.vhdchy.app.transport.PdaSession;
import vn.vhdchy.app.transport.ServiceEndpointPolicy;

/**
 * Persists only resumable bearer-session evidence. Passwords, OTP/TOTP material and
 * provider credentials do not belong in this store.
 */
public final class AndroidPdaSessionStore {
    private static final String PREFS_NAME = "vhdchy_pda_session_v1";
    private static final String KEY_ALIAS = "vhdchy.pda.session.v1";
    private static final String ANDROID_KEY_STORE = "AndroidKeyStore";
    private static final String CIPHER_TRANSFORMATION = "AES/GCM/NoPadding";
    private static final String PREF_FORMAT = "format";
    private static final String PREF_IV = "iv";
    private static final String PREF_CIPHERTEXT = "ciphertext";
    private static final int FORMAT_VERSION = 1;
    private static final int GCM_TAG_BITS = 128;
    private static final int MAX_TOKEN_BYTES = 1_048_576;
    private static final byte[] AAD = "VHDCHY_PDA_SESSION_V1".getBytes(StandardCharsets.UTF_8);

    private final SharedPreferences preferences;

    public AndroidPdaSessionStore(Context context) {
        if (context == null) throw new IllegalArgumentException("SESSION_STORE_CONTEXT_REQUIRED");
        preferences = context.getApplicationContext().getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
    }

    /**
     * Replaces any prior persisted session. Failure leaves no persisted session behind.
     */
    public boolean save(PdaSession session, long nowEpochMs) {
        if (session == null) throw new IllegalArgumentException("SESSION_REQUIRED");
        PdaSession.Snapshot snapshot;
        try {
            snapshot = session.snapshot(nowEpochMs);
        } catch (RuntimeException error) {
            clearPersisted();
            return false;
        }

        // Never leave an older valid bearer blob behind if replacement fails.
        if (!clearPersisted()) return false;

        try {
            SecretKey key = getOrCreateKey();
            Cipher cipher = Cipher.getInstance(CIPHER_TRANSFORMATION);
            cipher.init(Cipher.ENCRYPT_MODE, key);
            cipher.updateAAD(AAD);
            byte[] ciphertext = cipher.doFinal(encode(snapshot));
            byte[] iv = cipher.getIV();
            if (iv == null || iv.length == 0 || ciphertext.length == 0) return false;

            boolean committed = preferences.edit()
                .putInt(PREF_FORMAT, FORMAT_VERSION)
                .putString(PREF_IV, Base64.encodeToString(iv, Base64.NO_WRAP))
                .putString(PREF_CIPHERTEXT, Base64.encodeToString(ciphertext, Base64.NO_WRAP))
                .commit();
            if (!committed) clearPersisted();
            return committed;
        } catch (Exception error) {
            clearPersisted();
            return false;
        }
    }

    /**
     * Restores only a still-valid session. Missing/corrupt/undecryptable/expired state is cleared.
     */
    public boolean restore(PdaSession session, long nowEpochMs) {
        if (session == null) throw new IllegalArgumentException("SESSION_REQUIRED");
        session.clear();

        int format = preferences.getInt(PREF_FORMAT, 0);
        String encodedIv = preferences.getString(PREF_IV, null);
        String encodedCiphertext = preferences.getString(PREF_CIPHERTEXT, null);
        if (format == 0 && encodedIv == null && encodedCiphertext == null) return false;
        if (format != FORMAT_VERSION || isBlank(encodedIv) || isBlank(encodedCiphertext)) {
            clearPersisted();
            return false;
        }

        try {
            SecretKey key = getExistingKey();
            if (key == null) {
                clearPersisted();
                return false;
            }

            byte[] iv = Base64.decode(encodedIv, Base64.NO_WRAP);
            byte[] ciphertext = Base64.decode(encodedCiphertext, Base64.NO_WRAP);
            if (iv.length == 0 || ciphertext.length == 0) {
                clearPersisted();
                return false;
            }

            Cipher cipher = Cipher.getInstance(CIPHER_TRANSFORMATION);
            cipher.init(Cipher.DECRYPT_MODE, key, new GCMParameterSpec(GCM_TAG_BITS, iv));
            cipher.updateAAD(AAD);
            PdaSession.Snapshot snapshot = decode(cipher.doFinal(ciphertext));

            if (!session.restore(snapshot, nowEpochMs)) {
                clearPersisted();
                return false;
            }
            return true;
        } catch (Exception error) {
            session.clear();
            clearPersisted();
            return false;
        }
    }

    public boolean clearPersisted() {
        return preferences.edit().clear().commit();
    }

    private static boolean isBlank(String value) {
        return value == null || value.trim().isEmpty();
    }

    private static byte[] encode(PdaSession.Snapshot snapshot) throws Exception {
        byte[] token = snapshot.bearerToken().getBytes(StandardCharsets.UTF_8);
        if (token.length < 32 || token.length > MAX_TOKEN_BYTES) {
            throw new IllegalArgumentException("SESSION_TOKEN_INVALID");
        }

        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        try (DataOutputStream output = new DataOutputStream(bytes)) {
            output.writeInt(FORMAT_VERSION);
            output.writeLong(snapshot.expiresAtEpochMs());
            output.writeUTF(snapshot.runtimeMode().name());
            output.writeInt(token.length);
            output.write(token);
        }
        return bytes.toByteArray();
    }

    private static PdaSession.Snapshot decode(byte[] plaintext) throws Exception {
        if (plaintext == null || plaintext.length == 0) throw new IllegalArgumentException("SESSION_SNAPSHOT_INVALID");

        try (ByteArrayInputStream bytes = new ByteArrayInputStream(plaintext);
             DataInputStream input = new DataInputStream(bytes)) {
            if (input.readInt() != FORMAT_VERSION) throw new IllegalArgumentException("SESSION_SNAPSHOT_VERSION_INVALID");
            long expiresAtEpochMs = input.readLong();
            String runtimeName = input.readUTF();
            int tokenLength = input.readInt();
            if (tokenLength < 32 || tokenLength > MAX_TOKEN_BYTES || tokenLength > bytes.available()) {
                throw new IllegalArgumentException("SESSION_SNAPSHOT_INVALID");
            }
            byte[] tokenBytes = new byte[tokenLength];
            input.readFully(tokenBytes);
            if (bytes.available() != 0) throw new IllegalArgumentException("SESSION_SNAPSHOT_INVALID");

            String token = new String(tokenBytes, StandardCharsets.UTF_8);
            ServiceEndpointPolicy.RuntimeMode runtimeMode = ServiceEndpointPolicy.RuntimeMode.valueOf(runtimeName);
            return new PdaSession.Snapshot(token, expiresAtEpochMs, runtimeMode);
        }
    }

    private static SecretKey getOrCreateKey() throws Exception {
        KeyStore keyStore = loadKeyStore();
        Key existing = keyStore.getKey(KEY_ALIAS, null);
        if (existing instanceof SecretKey secretKey) return secretKey;
        if (existing != null) keyStore.deleteEntry(KEY_ALIAS);

        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, ANDROID_KEY_STORE);
        generator.init(new KeyGenParameterSpec.Builder(
            KEY_ALIAS,
            KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT
        )
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setKeySize(256)
            .setRandomizedEncryptionRequired(true)
            .build());
        return generator.generateKey();
    }

    private static SecretKey getExistingKey() throws Exception {
        Key key = loadKeyStore().getKey(KEY_ALIAS, null);
        return key instanceof SecretKey secretKey ? secretKey : null;
    }

    private static KeyStore loadKeyStore() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(ANDROID_KEY_STORE);
        keyStore.load(null);
        return keyStore;
    }
}
