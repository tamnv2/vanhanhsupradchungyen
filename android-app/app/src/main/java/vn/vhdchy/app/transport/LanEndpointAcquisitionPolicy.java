package vn.vhdchy.app.transport;

import java.net.URI;
import java.util.HashSet;
import java.util.Optional;
import java.util.Set;

/**
 * Selects a LAN Service endpoint without changing Cloud/LAN authority.
 *
 * Approved order: cached healthy endpoint -> LAN discovery -> manual recovery.
 * Every candidate must be HTTPS and pass a caller-supplied health probe before selection.
 */
public final class LanEndpointAcquisitionPolicy {
    public enum Source {
        CACHE,
        DISCOVERY,
        MANUAL
    }

    public record Selection(URI endpoint, Source source, boolean shouldCache) {
        public Selection {
            if (endpoint == null) throw new IllegalArgumentException("LAN_ENDPOINT_REQUIRED");
            if (source == null) throw new IllegalArgumentException("LAN_ENDPOINT_SOURCE_REQUIRED");
        }
    }

    @FunctionalInterface
    public interface HealthProbe {
        boolean isHealthy(URI endpoint) throws Exception;
    }

    public Optional<Selection> acquire(
        String cachedEndpoint,
        Iterable<String> discoveredEndpoints,
        String manualEndpoint,
        HealthProbe probe
    ) {
        if (probe == null) throw new IllegalArgumentException("LAN_HEALTH_PROBE_REQUIRED");

        Set<URI> attempted = new HashSet<>();

        URI cached = normalizeOptional(cachedEndpoint, "LAN_CACHED_ENDPOINT_INVALID");
        if (cached != null) {
            attempted.add(cached);
            if (healthy(cached, probe)) {
                return Optional.of(new Selection(cached, Source.CACHE, false));
            }
        }

        if (discoveredEndpoints != null) {
            for (String candidate : discoveredEndpoints) {
                URI discovered = normalizeDiscovery(candidate);
                if (discovered == null || !attempted.add(discovered)) continue;
                if (healthy(discovered, probe)) {
                    return Optional.of(new Selection(discovered, Source.DISCOVERY, true));
                }
            }
        }

        if (manualEndpoint != null && !manualEndpoint.isBlank()) {
            URI manual = ServiceEndpointPolicy.normalizeHttpsBase(manualEndpoint, "MANUAL_ENDPOINT_INVALID");
            if (!attempted.add(manual)) return Optional.empty();
            if (healthy(manual, probe)) {
                return Optional.of(new Selection(manual, Source.MANUAL, true));
            }
        }

        return Optional.empty();
    }

    private static URI normalizeOptional(String value, String code) {
        if (value == null || value.isBlank()) return null;
        try {
            return ServiceEndpointPolicy.normalizeHttpsBase(value, code);
        } catch (IllegalArgumentException ignored) {
            // A stale/corrupt cache is not authority. Ignore it and continue discovery.
            return null;
        }
    }

    private static URI normalizeDiscovery(String value) {
        if (value == null || value.isBlank()) return null;
        try {
            return ServiceEndpointPolicy.normalizeHttpsBase(value, "DISCOVERED_ENDPOINT_INVALID");
        } catch (IllegalArgumentException ignored) {
            // Discovery is untrusted network input. Invalid candidates are discarded.
            return null;
        }
    }

    private static boolean healthy(URI endpoint, HealthProbe probe) {
        try {
            return probe.isHealthy(endpoint);
        } catch (Exception ignored) {
            return false;
        }
    }
}
