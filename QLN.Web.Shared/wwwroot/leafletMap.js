window.initMap = (lat, lng) => {
   // const map = L.map('map-container').setView([lat, lng], 16);
    console.log(lat,lng);

    // initialize the map on the "map" div with a given center and zoom
    var map = L.map('map-container', {
        center: [lat, lng],
        zoom: 13
    });
    console.log(map);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    const customIcon = L.icon({
        iconUrl: '/qln-images/location_pin.svg',
        iconSize: [32, 48],
        iconAnchor: [16, 48],
        popupAnchor: [0, -40],
        shadowUrl: null
    });

    const marker = L.marker([lat, lng], {
        icon: customIcon,
        draggable: true
    }).addTo(map);

    marker.on('dragend', function (e) {
        const newPos = e.target.getLatLng();
        DotNet.invokeMethodAsync('QLN.Web.Shared', 'UpdateLatLng', newPos.lat, newPos.lng);
    });
};