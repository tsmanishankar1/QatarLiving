let leafletMap;
let leafletMarker;

window.initMap = (lat, lng) => {
    const center = [lat, lng];

    if (leafletMap) {
        leafletMap.setView(center, 16);
        if (leafletMarker) {
            leafletMarker.setLatLng(center);
        } else {
            leafletMarker = createMarker(center).addTo(leafletMap);
        }
        return;
    }

    leafletMap = L.map('map').setView(center, 16);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(leafletMap);

    leafletMarker = createMarker(center).addTo(leafletMap);
};

function createMarker(center) {
    const customIcon = L.icon({
        iconUrl: '/qln-images/location_pin.svg',
        iconSize: [32, 48],
        iconAnchor: [16, 48],
        popupAnchor: [0, -40]
    });

    const marker = L.marker(center, {
        icon: customIcon,
        draggable: true
    });

    marker.on('dragend', function (e) {
        const newPos = e.target.getLatLng();
        DotNet.invokeMethodAsync('QLN.ContentBO.WebUI', 'UpdateLatLng', newPos.lat, newPos.lng);
    });

    return marker;
}