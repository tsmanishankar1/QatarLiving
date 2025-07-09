window.resetLeafletMap = function () {
    if (window._leafletMap) {
        window._leafletMap.remove();
        window._leafletMap = null;
    }
    window._leafletMarker = null;
};

window.initializeMap = function (dotnetHelper) {
    if (window._leafletMap) return;

    const defaultLatLng = [25.276987, 55.296249]; // Dubai fallback

    const customIcon = L.icon({
        iconUrl: '/qln-images/location_pin.svg',
        iconSize: [32, 48],
        iconAnchor: [16, 48],
        popupAnchor: [0, -40]
    });

    function initMap(center) {
        const map = L.map('map').setView(center, 15);
        window._leafletMap = map;

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        window._leafletMarker = L.marker(center, { icon: customIcon }).addTo(map);

        // Send coordinates to Blazor
        dotnetHelper.invokeMethodAsync('SetCoordinates', center[0], center[1]);

        map.on('click', function (e) {
            const latlng = e.latlng;

            if (window._leafletMarker) {
                window._leafletMarker.setLatLng(latlng);
            } else {
                window._leafletMarker = L.marker(latlng, { icon: customIcon }).addTo(map);
            }

            // Notify .NET of coordinate change
            dotnetHelper.invokeMethodAsync('SetCoordinates', latlng.lat, latlng.lng);
        });
    }

    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                const userLatLng = [position.coords.latitude, position.coords.longitude];
                initMap(userLatLng);
            },
            (error) => {
                console.warn("Geolocation error:", error);
                initMap(defaultLatLng);
            },
            { enableHighAccuracy: true }
        );
    } else {
        initMap(defaultLatLng);
    }
};

window.updateMapCoordinates = function (lat, lng) {
    if (!window._leafletMap) return;

    const latlng = L.latLng(lat, lng);
    window._leafletMap.setView(latlng, 15);

    if (window._leafletMarker) {
        window._leafletMarker.setLatLng(latlng);
    } else {
        const customIcon = L.icon({
            iconUrl: '/qln-images/location_pin.svg',
            iconSize: [32, 48],
            iconAnchor: [16, 48],
            popupAnchor: [0, -40]
        });

        window._leafletMarker = L.marker(latlng, { icon: customIcon }).addTo(window._leafletMap);
    }
};
