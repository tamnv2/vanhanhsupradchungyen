package vn.vhdchy.app.transport;

import java.net.URI;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.Map;

public final class PdaRequestPlan {
    private final URI uri;
    private final Map<String, String> headers;

    private PdaRequestPlan(URI uri, Map<String, String> headers) {
        this.uri = uri;
        this.headers = Collections.unmodifiableMap(new LinkedHashMap<>(headers));
    }

    public static PdaRequestPlan authenticated(
        ServiceEndpointPolicy endpoints,
        PdaSession session,
        String apiPath,
        long nowEpochMs
    ) {
        if (endpoints == null || session == null) throw new IllegalArgumentException("REQUEST_CONTEXT_REQUIRED");
        ServiceEndpointPolicy.RuntimeMode mode = session.runtimeMode(nowEpochMs);
        URI uri = endpoints.resolve(mode, apiPath);
        Map<String, String> headers = new LinkedHashMap<>();
        headers.put("Authorization", session.authorizationHeader(nowEpochMs));
        headers.put("Accept", "application/json");
        headers.put("Content-Type", "application/json; charset=utf-8");
        return new PdaRequestPlan(uri, headers);
    }

    public URI uri() { return uri; }
    public Map<String, String> headers() { return headers; }
}
