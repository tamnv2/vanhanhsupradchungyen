package vn.vhdchy.transport;

import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.HttpURLConnection;
import java.net.InetAddress;
import java.net.URL;
import java.nio.charset.StandardCharsets;

final class LanTransportClient {
    static final int HTTP_PORT = 17891;
    static final int DISCOVERY_PORT = 17892;
    static final String DISCOVERY_MESSAGE = "VHDCHY_DISCOVER_CURRENT_BETA_V1";
    static final String SERVICE = "VHDCHY_LAN_AGENT";
    static final String ENVIRONMENT = "BETA";
    static final String PROTOCOL = "VHDCHY_LAN_TRANSPORT_TEST_V1";

    static Health discover(TransportRepository repo) {
        String cached = repo.cachedEndpoint();
        if (!cached.isEmpty()) {
            Health health = health(cached, "CACHE");
            if (health != null) return health;
        }

        DatagramSocket socket = null;
        try {
            socket = new DatagramSocket();
            socket.setBroadcast(true);
            socket.setSoTimeout(1200);
            byte[] request = DISCOVERY_MESSAGE.getBytes(StandardCharsets.UTF_8);
            socket.send(new DatagramPacket(request, request.length, InetAddress.getByName("255.255.255.255"), DISCOVERY_PORT));

            byte[] buffer = new byte[2048];
            DatagramPacket response = new DatagramPacket(buffer, buffer.length);
            socket.receive(response);
            JSONObject body = new JSONObject(new String(response.getData(), 0, response.getLength(), StandardCharsets.UTF_8));
            if (!body.optBoolean("ok")) return null;
            if (!SERVICE.equals(body.optString("service"))) return null;
            if (!ENVIRONMENT.equals(body.optString("environment"))) return null;
            if (!PROTOCOL.equals(body.optString("protocol"))) return null;
            int port = body.optInt("httpPort", HTTP_PORT);
            String endpoint = "http://" + response.getAddress().getHostAddress() + ":" + port;
            Health health = health(endpoint, "UDP");
            if (health != null) repo.setCachedEndpoint(endpoint);
            return health;
        } catch (Exception ignored) {
            return null;
        } finally {
            if (socket != null) socket.close();
        }
    }

    static Health health(String endpoint, String source) {
        try {
            JSONObject body = getJson(endpoint + "/health", 1200, 1800);
            if (!body.optBoolean("ok")) return null;
            if (!SERVICE.equals(body.optString("service"))) return null;
            if (!ENVIRONMENT.equals(body.optString("environment"))) return null;
            if (!PROTOCOL.equals(body.optString("protocol"))) return null;
            return new Health(endpoint, source, body.optString("instanceId"), body.optString("streamEpoch"), body.optString("version"));
        } catch (Exception ignored) {
            return null;
        }
    }

    static double echo(String endpoint, String deviceId) throws Exception {
        long start = System.nanoTime();
        JSONObject request = new JSONObject().put("deviceId", deviceId).put("payload", "transport-test-only");
        JSONObject result = postJson(endpoint + "/api/transport/echo", request, 1500, 2500);
        if (!result.optBoolean("ok")) throw new IllegalStateException("echo rejected");
        return (System.nanoTime() - start) / 1_000_000d;
    }

    static Ack sendTestEvent(String endpoint, String deviceId, TransportRepository.Pending pending) throws Exception {
        JSONObject request = new JSONObject()
                .put("idempotencyKey", pending.idempotencyKey)
                .put("deviceId", deviceId)
                .put("deviceSeq", pending.deviceSeq)
                .put("payload", pending.payload)
                .put("testOnly", true);
        JSONObject result = postJson(endpoint + "/api/transport/test-event", request, 1500, 3000);
        if (!result.optBoolean("ok")) throw new IllegalStateException(result.optString("code", "TEST_EVENT_REJECTED"));
        return new Ack(result.optString("ackType"), result.optBoolean("duplicate"));
    }

    private static JSONObject getJson(String url, int connectTimeout, int readTimeout) throws Exception {
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        connection.setConnectTimeout(connectTimeout);
        connection.setReadTimeout(readTimeout);
        connection.setUseCaches(false);
        connection.setRequestProperty("Accept", "application/json");
        int code = connection.getResponseCode();
        String text = readText(code >= 200 && code < 300 ? connection.getInputStream() : connection.getErrorStream());
        connection.disconnect();
        if (code < 200 || code >= 300) throw new IllegalStateException("HTTP " + code + " " + text);
        return new JSONObject(text);
    }

    private static JSONObject postJson(String url, JSONObject body, int connectTimeout, int readTimeout) throws Exception {
        byte[] bytes = body.toString().getBytes(StandardCharsets.UTF_8);
        HttpURLConnection connection = (HttpURLConnection) new URL(url).openConnection();
        connection.setRequestMethod("POST");
        connection.setDoOutput(true);
        connection.setConnectTimeout(connectTimeout);
        connection.setReadTimeout(readTimeout);
        connection.setUseCaches(false);
        connection.setRequestProperty("Content-Type", "application/json; charset=utf-8");
        connection.setFixedLengthStreamingMode(bytes.length);
        try (OutputStream out = connection.getOutputStream()) {
            out.write(bytes);
        }
        int code = connection.getResponseCode();
        String text = readText(code >= 200 && code < 300 ? connection.getInputStream() : connection.getErrorStream());
        connection.disconnect();
        if (code < 200 || code >= 300) throw new IllegalStateException("HTTP " + code + " " + text);
        return new JSONObject(text);
    }

    private static String readText(InputStream input) throws Exception {
        if (input == null) return "";
        StringBuilder out = new StringBuilder();
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(input, StandardCharsets.UTF_8))) {
            String line;
            while ((line = reader.readLine()) != null) out.append(line);
        }
        return out.toString();
    }

    static final class Health {
        final String endpoint;
        final String source;
        final String instanceId;
        final String streamEpoch;
        final String version;

        Health(String endpoint, String source, String instanceId, String streamEpoch, String version) {
            this.endpoint = endpoint;
            this.source = source;
            this.instanceId = instanceId;
            this.streamEpoch = streamEpoch;
            this.version = version;
        }
    }

    static final class Ack {
        final String ackType;
        final boolean duplicate;

        Ack(String ackType, boolean duplicate) {
            this.ackType = ackType;
            this.duplicate = duplicate;
        }
    }
}
