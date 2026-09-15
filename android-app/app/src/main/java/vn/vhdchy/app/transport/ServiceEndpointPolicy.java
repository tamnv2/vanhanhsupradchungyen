package vn.vhdchy.app.transport;

import java.net.URI;

public final class ServiceEndpointPolicy {
    public enum RuntimeMode { CLOUD_DIRECT, LAN_PRIMARY }

    private final URI cloudBase;
    private final URI lanBase;

    public ServiceEndpointPolicy(String cloudBase, String lanBase) {
        this.cloudBase = normalizeHttpsBase(cloudBase, "CLOUD_ENDPOINT_INVALID");
        this.lanBase = normalizeHttpsBase(lanBase, "LAN_ENDPOINT_INVALID");
    }

    public URI select(RuntimeMode mode) {
        if (mode == null) throw new IllegalArgumentException("RUNTIME_MODE_REQUIRED");
        return mode == RuntimeMode.CLOUD_DIRECT ? cloudBase : lanBase;
    }

    public URI resolve(RuntimeMode mode, String apiPath) {
        if (apiPath == null || !apiPath.startsWith("/api/v1/") || apiPath.contains("..")) {
            throw new IllegalArgumentException("API_PATH_INVALID");
        }
        URI base = select(mode);
        return URI.create(base.getScheme() + "://" + base.getAuthority() + apiPath);
    }

    static URI normalizeHttpsBase(String value, String code) {
        if (value == null || value.isBlank()) throw new IllegalArgumentException(code);
        URI uri;
        try {
            uri = URI.create(value.trim());
        } catch (RuntimeException error) {
            throw new IllegalArgumentException(code, error);
        }
        if (!"https".equalsIgnoreCase(uri.getScheme()) || uri.getHost() == null || uri.getHost().isBlank()) {
            throw new IllegalArgumentException(code);
        }
        if (uri.getUserInfo() != null || uri.getQuery() != null || uri.getFragment() != null) {
            throw new IllegalArgumentException(code);
        }
        String path = uri.getPath();
        if (path != null && !path.isEmpty() && !"/".equals(path)) throw new IllegalArgumentException(code);
        return URI.create("https://" + uri.getAuthority());
    }
}
