// 地图功能主文件 - 提取自 map.html
// 全局变量
let map;
let tileLayer;
let tripLayers = [];
let stationMarkers = [];
let currentTickets = [];

// 时间轴相关变量
let timelineYears = [];
let currentYearFilter = null;

// 聚合相关变量
let clusterMarkers = [];
let clusterEnabled = true;
let clusterZoomThreshold = 6;

// 当前popup状态
let currentPopupTicket = null;
let currentPopup = null;

// 悬停卡片专用变量
let hoverPopup = null;
let hoverPopupTicket = null;

// 双击检测相关变量
let clickTimer = null;
let isDoubleClick = false;
const DOUBLE_CLICK_DELAY = 350;

// 当前主题设置
let currentTheme = {
    isDarkMode: false,
    fontSize: 14,
    colors: {
        completed: '#2E7D32',
        pending: '#1976D2',
        rescheduled: '#FF9800',
        refunded: '#616161',
        selected: '#FF5722',
        station: '#D32F2F'
    },
    lineWidth: {
        completed: 3,
        pending: 4,
        rescheduled: 3,
        refunded: 3,
        selected: 6
    },
    markerSize: 12,
    showStationLabels: true,
    showDateLabels: true,
    highlightSelectedTrip: true,
    showHoverCard: true,
    directionFilter: 'All'
};

// 地图交互设置
let mapInteractions = {
    enableMouseWheelZoom: true,
    enableLeftDragPan: true,
    enableRightClickReset: true,
    zoomSensitivity: 120,
    enablePanInertia: true,
    doubleClickTripAction: 'OpenTicketEdit',
    doubleClickStationAction: 'ShowStationTickets',
    doubleClickBlankAction: 'ZoomInMap'
};

// 地图源配置
const tileSources = {
    osm: {
        url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
        attribution: '&copy; OpenStreetMap contributors'
    },
    dark: {
        url: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>'
    },
    amap: {
        url: 'https://webrd01.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=8&x={x}&y={y}&z={z}',
        attribution: '&copy; 高德地图'
    },
    satellite: {
        url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
        attribution: '&copy; Esri'
    }
};

// 存储已使用的标注位置
let dateLabelData = [];

// 记录同一路径上的行程
let routeTripCounts = new Map();

// 初始化地图
function initMap() {
    const mapOptions = {
        scrollWheelZoom: mapInteractions.enableMouseWheelZoom,
        dragging: mapInteractions.enableLeftDragPan,
        inertia: mapInteractions.enablePanInertia,
        doubleClickZoom: false,
        maxZoom: 16,
        minZoom: 4,
        maxBounds: [[10, 60], [60, 145]],
        maxBoundsViscosity: 1.0
    };

    map = L.map('map', mapOptions).setView([35.0, 105.0], 5);

    if (mapInteractions.enableMouseWheelZoom) {
        map.options.wheelPxPerZoomLevel = 60 * (100 / mapInteractions.zoomSensitivity);
    }

    setMapSource('osm');
    addLegend();

    map.on('click', function (e) {
        if (window.chrome?.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'clearSelection'
            }));
        }
    });

    map.on('popupclose', function(e) {
        currentPopup = null;
        currentPopupTicket = null;
    });

    map.on('dblclick', function (e) {
        handleBlankDoubleClick(e);
    });

    map.on('contextmenu', function (e) {
        if (mapInteractions.enableRightClickReset) {
            resetMapView();
        }
    });

    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'mapReady'
        }));
    }
}

// 重置地图视角
function resetMapView() {
    if (tripLayers.length > 0) {
        fitAllTrips();
    } else {
        map.setView([35.0, 105.0], 5);
    }
}

// 切换地图源
function setMapSource(sourceKey) {
    const source = tileSources[sourceKey];
    if (!source) return;

    if (tileLayer) {
        map.removeLayer(tileLayer);
    }

    const tileOptions = {
        attribution: source.attribution,
        maxZoom: 18
    };

    if (sourceKey !== 'amap') {
        tileOptions.subdomains = 'abc';
    }

    tileLayer = L.tileLayer(source.url, tileOptions).addTo(map);
    map.invalidateSize();
}

// 设置主题
function setTheme(themeSettings) {
    currentTheme.isDarkMode = themeSettings.isDarkMode || false;
    currentTheme.fontSize = themeSettings.fontSize || 14;

    const mapContainer = document.getElementById('map');
    if (currentTheme.isDarkMode) {
        mapContainer.style.backgroundColor = '#1e1e1e';
        document.body.classList.add('dark-mode');
    } else {
        mapContainer.style.backgroundColor = '#f5f5f5';
        document.body.classList.remove('dark-mode');
    }

    updateLegend();
}

// 设置地图交互
function setMapInteractions(interactionSettings) {
    if (interactionSettings.enableMouseWheelZoom !== undefined) {
        mapInteractions.enableMouseWheelZoom = interactionSettings.enableMouseWheelZoom;
        if (mapInteractions.enableMouseWheelZoom) {
            map.scrollWheelZoom.enable();
            map.options.wheelPxPerZoomLevel = 60 * (100 / mapInteractions.zoomSensitivity);
        } else {
            map.scrollWheelZoom.disable();
        }
    }

    if (interactionSettings.enableLeftDragPan !== undefined) {
        mapInteractions.enableLeftDragPan = interactionSettings.enableLeftDragPan;
        if (mapInteractions.enableLeftDragPan) {
            map.dragging.enable();
        } else {
            map.dragging.disable();
        }
    }

    if (interactionSettings.enableRightClickReset !== undefined) {
        mapInteractions.enableRightClickReset = interactionSettings.enableRightClickReset;
    }

    if (interactionSettings.zoomSensitivity !== undefined) {
        mapInteractions.zoomSensitivity = interactionSettings.zoomSensitivity;
        if (mapInteractions.enableMouseWheelZoom) {
            map.options.wheelPxPerZoomLevel = 60 * (100 / mapInteractions.zoomSensitivity);
        }
    }

    if (interactionSettings.enablePanInertia !== undefined) {
        mapInteractions.enablePanInertia = interactionSettings.enablePanInertia;
        map.options.inertia = mapInteractions.enablePanInertia;
    }

    if (interactionSettings.doubleClickTripAction !== undefined) {
        mapInteractions.doubleClickTripAction = interactionSettings.doubleClickTripAction;
    }

    if (interactionSettings.doubleClickStationAction !== undefined) {
        mapInteractions.doubleClickStationAction = interactionSettings.doubleClickStationAction;
    }

    if (interactionSettings.doubleClickBlankAction !== undefined) {
        mapInteractions.doubleClickBlankAction = interactionSettings.doubleClickBlankAction;
    }
}

// 设置地图样式
function setMapStyles(styleSettings) {
    if (styleSettings.colors) {
        Object.assign(currentTheme.colors, styleSettings.colors);
    }

    if (styleSettings.lineWidth) {
        Object.assign(currentTheme.lineWidth, styleSettings.lineWidth);
    }

    if (styleSettings.markerSize) {
        currentTheme.markerSize = styleSettings.markerSize;
    }

    if (styleSettings.showStationLabels !== undefined) {
        currentTheme.showStationLabels = styleSettings.showStationLabels;
    }
    if (styleSettings.showDateLabels !== undefined) {
        currentTheme.showDateLabels = styleSettings.showDateLabels;
    }
    if (styleSettings.highlightSelectedTrip !== undefined) {
        currentTheme.highlightSelectedTrip = styleSettings.highlightSelectedTrip;
    }
    if (styleSettings.showHoverCard !== undefined) {
        currentTheme.showHoverCard = styleSettings.showHoverCard;
    }
    if (styleSettings.directionFilter !== undefined) {
        currentTheme.directionFilter = styleSettings.directionFilter;
    }

    updateTripStyles();
    updateDirectionFilter();
    updateStationMarkerStyles();
    updateDateLabels();
    updateHoverCards();
    updateLegend();
}

// 更新所有行程的样式
function updateTripStyles() {
    tripLayers.forEach(item => {
        const color = getTripColor(item.ticket.status);
        const weight = getTripWeight(item.ticket.status);
        item.layer.setStyle({
            color: color,
            weight: weight,
            opacity: 0.8
        });
    });
}

// 更新日期标注显示
function updateDateLabels() {
    const filter = currentTheme.directionFilter;
    
    tripLayers.forEach(item => {
        const ticket = item.ticket;
        const ticketYear = safeGetYear(ticket.departDate);
        const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
        
        // 检查方向过滤条件
        let directionMatch = true;
        if (filter !== 'All') {
            directionMatch = shouldShowLayerForFilter(item, filter);
        }
        
        // 只有在符合所有过滤条件且设置显示日期标签时才显示
        const shouldShow = currentTheme.showDateLabels && yearMatch && directionMatch;
        
        if (item.dateLabelMarker) {
            if (shouldShow) {
                item.dateLabelMarker.addTo(map);
            } else {
                map.removeLayer(item.dateLabelMarker);
            }
        } else if (shouldShow) {
            const latlngs = [
                [ticket.departLat, ticket.departLng],
                [ticket.arriveLat, ticket.arriveLng]
            ];
            addDateLabel(ticket, latlngs);
        }
    });
}

// 更新悬停卡片显示
function updateHoverCards() {
    tripLayers.forEach(item => {
        const ticket = item.ticket;
        const weight = getTripWeight(ticket.status);
        
        item.layer.off('mouseover');
        item.layer.off('mouseout');
        item.layer.off('mousemove');
        item.layer.unbindPopup();

        if (currentTheme.showHoverCard) {
            const popup = L.popup({
                closeButton: false,
                offset: [0, -5],
                keepInView: false,
                autoPan: false,
                autoPanOnFocus: false,
                className: 'hover-popup'
            });

            item.layer.bindPopup(popup);

            item.layer.on('mouseover', function (e) {
                this.setStyle({ weight: weight * 2, opacity: 1 });
                
                if (currentPopup && currentPopupTicket) {
                    return;
                }

                if (window.popupCloseTimer) {
                    clearTimeout(window.popupCloseTimer);
                    window.popupCloseTimer = null;
                }

                hoverPopup = popup;
                hoverPopupTicket = item.ticket;
                popup.setContent(createPopupContent(item.ticket, true));
                popup.setLatLng(e.latlng);
                this.openPopup();
            });

            item.layer.on('mousemove', function (e) {
                if (hoverPopup === popup) {
                    popup.setLatLng(e.latlng);
                }
            });

            item.layer.on('mouseout', function (e) {
                this.setStyle({ weight: weight, opacity: 0.8 });
                
                window.popupCloseTimer = setTimeout(() => {
                    const popupElement = popup.getElement();
                    if (popupElement) {
                        const rect = popupElement.getBoundingClientRect();
                        const mouseX = e.originalEvent?.clientX;
                        const mouseY = e.originalEvent?.clientY;
                        if (mouseX >= rect.left - 10 && mouseX <= rect.right + 10 &&
                            mouseY >= rect.top - 10 && mouseY <= rect.bottom + 10) {
                            return;
                        }
                    }
                    if (hoverPopup === popup) {
                        this.closePopup();
                        hoverPopup = null;
                    }
                }, 500);
            });

            popup.on('add', function() {
                const popupElement = popup.getElement();
                if (popupElement && !popupElement._hoverEventsBound) {
                    popupElement._hoverEventsBound = true;
                    
                    popupElement.addEventListener('mouseenter', function() {
                        if (window.popupCloseTimer) {
                            clearTimeout(window.popupCloseTimer);
                            window.popupCloseTimer = null;
                        }
                    });
                    
                    popupElement.addEventListener('mouseleave', function() {
                        window.popupCloseTimer = setTimeout(() => {
                            if (hoverPopup === popup) {
                                item.layer.closePopup();
                                hoverPopup = null;
                            }
                        }, 100);
                    });
                }
            });
        } else {
            item.layer.on('mouseover', function () {
                this.setStyle({ weight: weight * 2, opacity: 1 });
            });
            
            item.layer.on('mouseout', function () {
                this.setStyle({ weight: weight, opacity: 0.8 });
            });
        }
    });
}

// 更新车站标记样式
function updateStationMarkerStyles() {
    stationMarkers.forEach(item => {
        const markerElement = item.marker.getElement();
        if (markerElement) {
            const markerDiv = markerElement.querySelector('.station-marker');
            if (markerDiv) {
                markerDiv.style.width = currentTheme.markerSize + 'px';
                markerDiv.style.height = currentTheme.markerSize + 'px';
                markerDiv.style.background = currentTheme.colors.station;
            }
        }
    });
    
    // 更新车站标签显示（由 updateStationsVisibility 统一处理）
    updateStationsVisibility();
}

// 更新图例
function updateLegend() {
    document.querySelectorAll('.legend').forEach(el => el.remove());
    addLegend();
}

// 判断车次方向
function getTrainDirection(trainNo) {
    const match = trainNo.match(/\d+/);
    if (!match) return 'unknown';
    const num = parseInt(match[0], 10);
    return num % 2 === 0 ? 'up' : 'down';
}

// 更新方向过滤
function updateDirectionFilter() {
    const filter = currentTheme.directionFilter;
    
    tripLayers.forEach(item => {
        const ticketYear = safeGetYear(item.ticket.departDate);
        const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
        
        // 检查方向过滤条件
        let directionMatch = true;
        if (filter !== 'All') {
            directionMatch = shouldShowLayerForFilter(item, filter);
        }
        
        const shouldShow = yearMatch && directionMatch;
        
        if (shouldShow) {
            item.layer.addTo(map);
            if (item.dateLabelMarker && currentTheme.showDateLabels) {
                item.dateLabelMarker.addTo(map);
            }
        } else {
            map.removeLayer(item.layer);
            if (item.dateLabelMarker) {
                map.removeLayer(item.dateLabelMarker);
            }
        }
    });
    
    // 更新车站可见性（包括标签）
    updateStationsVisibility();
    // 更新聚合显示
    updateClusters();
}

// 判断图层是否应该在当前过滤条件下显示
function shouldShowLayerForFilter(layerItem, filter) {
    const ticket = layerItem.ticket;
    
    if (!ticket.routeGroup || ticket.routeGroup.length <= 1) {
        const direction = getTrainDirection(ticket.trainNo);
        return (filter === 'Up' && direction === 'up') ||
               (filter === 'Down' && direction === 'down');
    }
    
    for (const groupedTicket of ticket.routeGroup) {
        const direction = getTrainDirection(groupedTicket.trainNo);
        if ((filter === 'Up' && direction === 'up') ||
            (filter === 'Down' && direction === 'down')) {
            return true;
        }
    }
    
    return false;
}

// 添加图例
function addLegend() {
    const legend = L.control({position: 'bottomright'});

    legend.onAdd = function (map) {
        const div = L.DomUtil.create('div', 'legend');

        if (currentTheme.isDarkMode) {
            div.style.backgroundColor = 'rgba(30,30,30,0.9)';
            div.style.color = '#ffffff';
        } else {
            div.style.backgroundColor = 'rgba(255,255,255,0.9)';
            div.style.color = '#333333';
        }

        const fontSize = Math.max(10, currentTheme.fontSize - 2);
        div.style.fontSize = fontSize + 'px';

        div.innerHTML = `
            <div style="font-weight:bold;margin-bottom:5px;">图例</div>
            <div class="legend-item">
                <div class="legend-color" style="background:${currentTheme.colors.completed}"></div>
                <span>已完成行程</span>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background:${currentTheme.colors.pending}"></div>
                <span>未出行行程</span>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background:${currentTheme.colors.rescheduled}"></div>
                <span>已改签行程</span>
            </div>
            <div class="legend-item">
                <div class="legend-color" style="background:${currentTheme.colors.refunded}"></div>
                <span>已退票行程</span>
            </div>
            <div class="legend-item">
                <div style="width:10px;height:10px;background:${currentTheme.colors.station};border-radius:50%;margin-right:8px;margin-left:5px;"></div>
                <span>车站</span>
            </div>
        `;
        return div;
    };

    legend.addTo(map);
}

// 接收WPF发送的数据
window.receiveData = function (data) {
    try {
        if (typeof data === 'string') {
            data = JSON.parse(data);
        }

        switch (data.type) {
            case 'loadTickets':
                loadTickets(data.tickets);
                if (data.missingStations && data.missingStations.length > 0) {
                    showMissingCoordsWarning(data.missingStations);
                } else {
                    hideMissingCoordsWarning();
                }
                break;
            case 'highlightTrips':
                highlightTrips(data.tripIds, data.fitView !== undefined ? data.fitView : true);
                break;
            case 'showTripInfoCard':
                showTripInfoCard(data.tripId);
                break;
            case 'selectTrip':
                selectTrip(data.tripId);
                break;
        }
    } catch (e) {
        console.error('处理数据失败:', e);
        sendError(e.message);
    }
};

// 显示缺少经纬度车站警告
function showMissingCoordsWarning(stations) {
    const warningEl = document.getElementById('missingCoordsWarning');
    const listEl = document.getElementById('missingStationsList');

    if (!warningEl || !listEl || !stations || stations.length === 0) return;

    listEl.innerHTML = '';

    const displayStations = stations.slice(0, 10);
    displayStations.forEach(station => {
        const li = document.createElement('li');
        li.textContent = station;
        listEl.appendChild(li);
    });

    if (stations.length > 10) {
        const li = document.createElement('li');
        li.textContent = `... 等共 ${stations.length} 个车站`;
        li.style.fontStyle = 'italic';
        li.style.color = '#999';
        listEl.appendChild(li);
    }

    warningEl.classList.add('visible');
}

// 隐藏缺少经纬度车站警告
function hideMissingCoordsWarning() {
    const warningEl = document.getElementById('missingCoordsWarning');
    if (warningEl) {
        warningEl.classList.remove('visible');
    }
}

// 加载车票数据
function loadTickets(tickets) {
    clearLayers();
    clearUsedLabelPositions();
    routeTripCounts.clear();
    currentYearFilter = null;

    currentTickets = tickets;

    initTimeline(tickets);
    const stationMap = new Map();

    const filter = currentTheme.directionFilter;
    if (filter !== 'All') {
        // 先按路线分组，检查每组内是否有符合方向过滤条件的车次
        const tempRouteGroups = new Map();
        tickets.forEach(ticket => {
            const routeKey = `${ticket.departStation}-${ticket.arriveStation}-${ticket.trainNo}`;
            if (!tempRouteGroups.has(routeKey)) {
                tempRouteGroups.set(routeKey, []);
            }
            tempRouteGroups.get(routeKey).push(ticket);
        });

        // 过滤：保留那些在路线组内有至少一个车次符合方向过滤条件的票
        tickets = tickets.filter(ticket => {
            const routeKey = `${ticket.departStation}-${ticket.arriveStation}-${ticket.trainNo}`;
            const group = tempRouteGroups.get(routeKey) || [ticket];
            
            // 检查该路线组内是否有符合方向过滤条件的车次
            for (const groupedTicket of group) {
                const direction = getTrainDirection(groupedTicket.trainNo);
                if ((filter === 'Up' && direction === 'up') ||
                    (filter === 'Down' && direction === 'down')) {
                    return true;
                }
            }
            return false;
        });
    }

    const routeGroups = new Map();
    const stationPairGroups = new Map();

    tickets.forEach(ticket => {
        if (!stationMap.has(ticket.departStation)) {
            stationMap.set(ticket.departStation, {
                name: ticket.departStation,
                lat: ticket.departLat,
                lng: ticket.departLng
            });
        }
        if (!stationMap.has(ticket.arriveStation)) {
            stationMap.set(ticket.arriveStation, {
                name: ticket.arriveStation,
                lat: ticket.arriveLat,
                lng: ticket.arriveLng
            });
        }

        const routeKey = `${ticket.departStation}-${ticket.arriveStation}-${ticket.trainNo}`;
        if (!routeGroups.has(routeKey)) {
            routeGroups.set(routeKey, []);
        }
        routeGroups.get(routeKey).push(ticket);

        const pairKey = `${ticket.departStation}-${ticket.arriveStation}`;
        if (!stationPairGroups.has(pairKey)) {
            stationPairGroups.set(pairKey, []);
        }
        if (!stationPairGroups.get(pairKey).includes(ticket.trainNo)) {
            stationPairGroups.get(pairKey).push(ticket.trainNo);
        }
    });

    tickets.forEach(ticket => {
        const routeKey = `${ticket.departStation}-${ticket.arriveStation}-${ticket.trainNo}`;
        const group = routeGroups.get(routeKey);
        const index = group.indexOf(ticket);
        ticket.routeGroup = group;
        ticket.routeIndex = index;
        ticket.routeTotal = group.length;

        const pairKey = `${ticket.departStation}-${ticket.arriveStation}`;
        const pairGroup = stationPairGroups.get(pairKey);
        ticket.pairIndex = pairGroup.indexOf(ticket.trainNo);
        ticket.pairTotal = pairGroup.length;
    });

    const drawnRouteKeys = new Set();
    tickets.forEach(ticket => {
        const routeKey = `${ticket.departStation}-${ticket.arriveStation}-${ticket.trainNo}`;
        if (!drawnRouteKeys.has(routeKey)) {
            drawnRouteKeys.add(routeKey);
            drawTrip(ticket);
        }
    });

    stationMap.forEach(station => {
        addStationMarker(station);
    });

    if (tickets.length > 0) {
        fitAllTrips();
    }

    setTimeout(() => {
        updateClusters();
    }, 100);

    updateHoverCards();
}

// 计算贝塞尔曲线点
function calculateBezierPoints(start, end, index, total) {
    if (start[0] === end[0] && start[1] === end[1]) {
        return [start, end];
    }

    if (total <= 1) {
        return [start, end];
    }

    const midLat = (start[0] + end[0]) / 2;
    const midLng = (start[1] + end[1]) / 2;

    const dx = end[1] - start[1];
    const dy = end[0] - start[0];
    const length = Math.sqrt(dx * dx + dy * dy);

    if (length === 0) {
        return [start, end];
    }

    const perpX = -dy / length;
    const perpY = dx / length;

    const curveAmount = 0.05;
    let offsetMultiplier;

    if (total % 2 === 0) {
        offsetMultiplier = (index < total / 2) ?
            -(total / 2 - index) + 0.5 :
            (index - total / 2) + 0.5;
    } else {
        const middleIndex = Math.floor(total / 2);
        if (index === middleIndex) {
            offsetMultiplier = 0;
        } else if (index < middleIndex) {
            offsetMultiplier = -(middleIndex - index);
        } else {
            offsetMultiplier = index - middleIndex;
        }
    }

    const controlLat = midLat + perpY * curveAmount * offsetMultiplier;
    const controlLng = midLng + perpX * curveAmount * offsetMultiplier;

    const points = [];
    const segments = 20;
    for (let i = 0; i <= segments; i++) {
        const t = i / segments;
        const lat = (1 - t) * (1 - t) * start[0] + 2 * (1 - t) * t * controlLat + t * t * end[0];
        const lng = (1 - t) * (1 - t) * start[1] + 2 * (1 - t) * t * controlLng + t * t * end[1];
        points.push([lat, lng]);
    }

    return points;
}

// 绘制单个行程
function drawTrip(ticket) {
    const color = getTripColor(ticket.status);
    const weight = getTripWeight(ticket.status);

    const start = [ticket.departLat, ticket.departLng];
    const end = [ticket.arriveLat, ticket.arriveLng];

    const pairIndex = ticket.pairIndex || 0;
    const pairTotal = ticket.pairTotal || 1;

    const latlngs = calculateBezierPoints(start, end, pairIndex, pairTotal);

    const polyline = L.polyline(latlngs, {
        color: color,
        weight: weight,
        opacity: 0.8,
        smoothFactor: 1,
        noWrap: true
    }).addTo(map);

    polyline.on('click', function (e) {
        L.DomEvent.stopPropagation(e);

        if (clickTimer) {
            clearTimeout(clickTimer);
            clickTimer = null;
            return;
        }

        clickTimer = setTimeout(() => {
            clickTimer = null;
            executeTripClick(e, ticket, start, end);
        }, DOUBLE_CLICK_DELAY);
    });

    polyline.on('dblclick', function (e) {
        L.DomEvent.stopPropagation(e);

        if (clickTimer) {
            clearTimeout(clickTimer);
            clickTimer = null;
        }

        handleTripDoubleClick(ticket);
    });

    function executeTripClick(e, ticket, start, end) {
        sendTripClick(ticket.id);
        highlightTrips([ticket.id]);

        if (currentPopup) {
            map.closePopup();
        }

        currentPopupTicket = ticket;

        const clickLatLng = e.latlng;
        const bounds = map.getBounds();
        let popupLatLng;

        if (bounds.contains(clickLatLng)) {
            popupLatLng = clickLatLng;
        } else {
            const midLat = (start[0] + end[0]) / 2;
            const midLng = (start[1] + end[1]) / 2;
            popupLatLng = L.latLng(midLat, midLng);
        }

        const popup = L.popup({
            closeButton: true,
            offset: [0, -10],
            keepInView: true,
            autoPan: true,
            autoPanPadding: [50, 50]
        });

        popup.setContent(createPopupContent(ticket));
        popup.setLatLng(popupLatLng);
        popup.openOn(map);
        currentPopup = popup;
        
        popup.on('popupclose', function() {
            currentPopup = null;
            currentPopupTicket = null;
        });
    }

    const tripLayer = {
        id: ticket.id,
        layer: polyline,
        ticket: ticket
    };
    tripLayers.push(tripLayer);

    if (currentTheme.showDateLabels) {
        addDateLabel(ticket, latlngs);
    }
}

// 添加日期标注
function addDateLabel(ticket, latlngs) {
    dateLabelData.push({
        ticket: ticket,
        start: L.latLng(latlngs[0]),
        end: L.latLng(latlngs[1])
    });

    createDateLabelMarker(ticket, latlngs);
}

// 创建日期标签 marker
function createDateLabelMarker(ticket, latlngs) {
    const start = L.latLng(latlngs[0]);
    const end = L.latLng(latlngs[1]);
    
    const startPoint = map.latLngToContainerPoint(start);
    const endPoint = map.latLngToContainerPoint(end);
    
    const midX = (startPoint.x + endPoint.x) / 2;
    const midY = (startPoint.y + endPoint.y) / 2;
    
    const midLatLng = map.containerPointToLatLng([midX, midY]);

    const dateLabelIcon = L.divIcon({
        className: 'date-label-simple',
        html: ticket.departDate,
        iconSize: null,
        iconAnchor: null
    });

    const dateLabelMarker = L.marker(
        [midLatLng.lat, midLatLng.lng],
        {icon: dateLabelIcon, interactive: false}
    ).addTo(map);

    const tripLayer = tripLayers.find(t => t.id === ticket.id);
    if (tripLayer) {
        if (tripLayer.dateLabelMarker) {
            map.removeLayer(tripLayer.dateLabelMarker);
        }
        tripLayer.dateLabelMarker = dateLabelMarker;
    }
}

// 重新计算所有日期标签位置
function updateAllDateLabels() {
    const filter = currentTheme.directionFilter;
    
    dateLabelData.forEach(data => {
        const ticket = data.ticket;
        const ticketYear = safeGetYear(ticket.departDate);
        const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
        
        // 同时检查方向过滤条件
        let directionMatch = true;
        if (filter !== 'All') {
            const tripLayer = tripLayers.find(item => item.id === ticket.id);
            if (tripLayer) {
                directionMatch = shouldShowLayerForFilter(tripLayer, filter);
            }
        }
        
        // 只有符合过滤条件的才创建日期标签
        if (yearMatch && directionMatch) {
            const latlngs = [[data.start.lat, data.start.lng], [data.end.lat, data.end.lng]];
            createDateLabelMarker(data.ticket, latlngs);
        }
    });
}

// 清除已使用的标注位置
function clearUsedLabelPositions() {
    dateLabelData = [];
}

// 添加车站标记
function addStationMarker(station) {
    const size = currentTheme.markerSize || 12;
    const halfSize = size / 2;

    const icon = L.divIcon({
        className: 'station-marker-container',
        html: `<div class="station-marker" style="width:${size}px;height:${size}px;background:${currentTheme.colors.station};border:2px solid white;border-radius:50%;box-shadow:0 2px 5px rgba(0,0,0,0.3);"></div>`,
        iconSize: [size, size],
        iconAnchor: [halfSize, halfSize]
    });

    const marker = L.marker([station.lat, station.lng], {icon: icon}).addTo(map);

    const labelIcon = L.divIcon({
        className: 'station-label-container',
        html: `<div class="station-label">${station.name}</div>`,
        iconSize: [80, 20],
        iconAnchor: [40, -10]
    });

    const labelMarker = L.marker([station.lat, station.lng], {icon: labelIcon, interactive: false});

    if (currentTheme.showStationLabels) {
        labelMarker.addTo(map);
    }

    marker.on('click', function (e) {
        L.DomEvent.stopPropagation(e);
        sendStationClick(station.name);
    });

    marker.on('dblclick', function (e) {
        L.DomEvent.stopPropagation(e);
        handleStationDoubleClick(station.name);
    });

    stationMarkers.push({
        name: station.name,
        lat: station.lat,
        lng: station.lng,
        marker: marker,
        labelMarker: labelMarker
    });
}

// 切换到上一张车票
function switchToPrevTicket() {
    const isHover = !currentPopup && hoverPopup;
    const currentTicket = isHover ? hoverPopupTicket : currentPopupTicket;
    
    if (!currentTicket || !currentTicket.routeGroup) return;
    const group = currentTicket.routeGroup;
    let newIndex = currentTicket.routeIndex;
    
    for (let i = 0; i < group.length; i++) {
        newIndex--;
        if (newIndex < 0) newIndex = group.length - 1;
        
        const newTicket = group[newIndex];
        if (isTicketAllowedByFilter(newTicket)) {
            updatePopupContent(newTicket);
            return;
        }
    }
    
    updatePopupContent(currentTicket);
}

// 切换到下一张车票
function switchToNextTicket() {
    const isHover = !currentPopup && hoverPopup;
    const currentTicket = isHover ? hoverPopupTicket : currentPopupTicket;
    
    if (!currentTicket || !currentTicket.routeGroup) return;
    const group = currentTicket.routeGroup;
    let newIndex = currentTicket.routeIndex;
    
    for (let i = 0; i < group.length; i++) {
        newIndex++;
        if (newIndex >= group.length) newIndex = 0;
        
        const newTicket = group[newIndex];
        if (isTicketAllowedByFilter(newTicket)) {
            updatePopupContent(newTicket);
            return;
        }
    }
    
    updatePopupContent(currentTicket);
}

// 检查车票是否符合当前方向过滤条件
function isTicketAllowedByFilter(ticket) {
    const filter = currentTheme.directionFilter;
    if (filter === 'All') return true;
    
    const direction = getTrainDirection(ticket.trainNo);
    return (filter === 'Up' && direction === 'up') ||
           (filter === 'Down' && direction === 'down');
}

// 更新popup内容
function updatePopupContent(ticket) {
    if (currentPopup) {
        currentPopupTicket = ticket;
        currentPopup.setContent(createPopupContent(ticket));
    } else if (hoverPopup) {
        hoverPopupTicket = ticket;
        hoverPopup.setContent(createPopupContent(ticket, true));
    }
}

// 创建弹出内容
function createPopupContent(ticket, isHover = false) {
    let statusClass = 'status-completed';
    if (ticket.status === '未出行') statusClass = 'status-pending';
    else if (ticket.status === '已改签') statusClass = 'status-rescheduled';
    else if (ticket.status === '已退票') statusClass = 'status-refunded';

    let titleHtml = `<h3>${ticket.trainNo}</h3>`;
    if (ticket.routeTotal > 1) {
        titleHtml = `
            <div style="display: flex; align-items: center; justify-content: space-between; gap: 10px;">
                <button class="nav-btn" onclick="event.stopPropagation(); switchToPrevTicket();">◀</button>
                <h3 style="margin: 0; flex: 1; text-align: center;">${ticket.trainNo} (${ticket.routeIndex + 1}/${ticket.routeTotal})</h3>
                <button class="nav-btn" onclick="event.stopPropagation(); switchToNextTicket();">▶</button>
            </div>
        `;
    }

    return `
        <div class="trip-popup">
            ${titleHtml}
            <div class="info-row">
                <span class="label">出发站</span>
                <span class="value">${ticket.departStation}</span>
            </div>
            <div class="info-row">
                <span class="label">到达站</span>
                <span class="value">${ticket.arriveStation}</span>
            </div>
            <div class="info-row">
                <span class="label">出发时间</span>
                <span class="value">${ticket.departDate} ${ticket.departTime}</span>
            </div>
            <div class="info-row">
                <span class="label">到达时间</span>
                <span class="value">${ticket.arriveTime}</span>
            </div>
            <div class="info-row">
                <span class="label">座位类型</span>
                <span class="value">${ticket.seatType}</span>
            </div>
            <div class="info-row">
                <span class="label">票价</span>
                <span class="value">¥${ticket.price}</span>
            </div>
            <div class="info-row">
                <span class="label">状态</span>
                <span class="value ${statusClass}">${ticket.status}</span>
            </div>
        </div>
    `;
}

// 获取行程颜色
function getTripColor(status) {
    switch (status) {
        case '已完成': return currentTheme.colors.completed;
        case '未出行': return currentTheme.colors.pending;
        case '已改签': return currentTheme.colors.rescheduled;
        case '已退票': return currentTheme.colors.refunded;
        default: return currentTheme.colors.completed;
    }
}

// 获取行程线宽
function getTripWeight(status) {
    switch (status) {
        case '已完成': return currentTheme.lineWidth.completed;
        case '未出行': return currentTheme.lineWidth.pending;
        case '已改签': return currentTheme.lineWidth.rescheduled;
        case '已退票': return currentTheme.lineWidth.refunded;
        default: return currentTheme.lineWidth.completed;
    }
}

// 处理行程双击事件
function handleTripDoubleClick(ticket) {
    const action = mapInteractions.doubleClickTripAction || 'OpenTicketEdit';

    switch (action) {
        case 'OpenTicketEdit':
            sendOpenTicketEdit(ticket.id);
            break;
        case 'PreviewTicket':
            sendPreviewTicket(ticket.id);
            break;
    }
}

// 处理车站双击事件
function handleStationDoubleClick(stationName) {
    const action = mapInteractions.doubleClickStationAction || 'ShowStationTickets';

    switch (action) {
        case 'ShowStationTickets':
            sendShowStationTickets(stationName);
            break;
        case 'LocateStation':
            zoomToStation(stationName);
            break;
        case 'ShowStationStats':
            sendShowStationStats(stationName);
            break;
        case 'CreateTicketFromStation':
            sendCreateTicketFromStation(stationName);
            break;
    }
}

// 处理地图空白处双击事件
function handleBlankDoubleClick(e) {
    L.DomEvent.stopPropagation(e);
    L.DomEvent.preventDefault(e);

    const action = mapInteractions.doubleClickBlankAction || 'ZoomInMap';

    switch (action) {
        case 'ZoomInMap':
            map.zoomIn();
            break;
        case 'ResetToChinaView':
            map.setView([35.0, 105.0], 5);
            break;
        case 'FitAllTrips':
            fitAllTrips();
            break;
        case 'CreateNewTicket':
            sendCreateNewTicket();
            break;
        case 'ClearSelection':
            clearSelection();
            break;
    }
}

// 缩放到指定行程
function zoomToTrip(ticket) {
    const latlngs = [
        [ticket.departLat, ticket.departLng],
        [ticket.arriveLat, ticket.arriveLng]
    ];
    const bounds = L.latLngBounds(latlngs);
    map.fitBounds(bounds.pad(0.2));
}

// 缩放到指定车站
function zoomToStation(stationName) {
    const stationItem = stationMarkers.find(item => item.name === stationName);
    if (stationItem) {
        map.setView([stationItem.lat, stationItem.lng], 10);
    }
}

// 高亮显示指定行程
function highlightTrips(tripIds, fitView = true, isFromDoubleClick = false) {
    if (!currentTheme.highlightSelectedTrip) {
        return;
    }

    tripLayers.forEach(item => {
        const color = getTripColor(item.ticket.status);
        const weight = getTripWeight(item.ticket.status);
        item.layer.setStyle({
            color: color,
            weight: weight,
            opacity: 0.8
        });
    });

    const highlightedLayers = [];
    tripLayers.forEach(item => {
        if (tripIds.includes(item.id)) {
            item.layer.setStyle({
                color: currentTheme.colors.selected,
                weight: currentTheme.lineWidth.selected,
                opacity: 1
            });
            item.layer.bringToFront();
            highlightedLayers.push(item);
        }
    });

    if (fitView && !isFromDoubleClick && highlightedLayers.length > 0) {
        fitHighlightedTrips(highlightedLayers);
    }
}

// 显示行程信息卡片
function showTripInfoCard(tripId) {
    const tripLayer = tripLayers.find(item => item.id === tripId);
    if (!tripLayer) {
        const ticketInfo = currentTickets.find(t => t.id === tripId);
        if (ticketInfo) {
            const sameTrainLayer = tripLayers.find(item => 
                item.ticket.trainNo === ticketInfo.trainNo &&
                item.ticket.departStation === ticketInfo.departStation &&
                item.ticket.arriveStation === ticketInfo.arriveStation
            );
            if (sameTrainLayer) {
                showTripInfoCardWithTicket(ticketInfo, sameTrainLayer);
                return;
            }
        }
        return;
    }

    const ticket = currentTickets.find(t => t.id === tripId) || tripLayer.ticket;
    
    showTripInfoCardWithTicket(ticket, tripLayer);
}

// 显示行程信息卡片（带车票数据）
function showTripInfoCardWithTicket(ticket, tripLayer) {
    const start = [ticket.departLat, ticket.departLng];
    const end = [ticket.arriveLat, ticket.arriveLng];

    if (currentPopup) {
        map.closePopup();
    }

    currentPopupTicket = ticket;

    const midLat = (start[0] + end[0]) / 2;
    const midLng = (start[1] + end[1]) / 2;
    const popupLatLng = L.latLng(midLat, midLng);

    const popup = L.popup({
        closeButton: true,
        offset: [0, -10],
        keepInView: true,
        autoPan: true,
        autoPanPadding: [50, 50]
    });

    popup.setContent(createPopupContent(ticket));
    popup.setLatLng(popupLatLng);
    popup.openOn(map);
    currentPopup = popup;
    
    popup.on('popupclose', function() {
        currentPopup = null;
        currentPopupTicket = null;
    });
}

// 选中行程
function selectTrip(tripId) {
    let tripLayer = tripLayers.find(item => item.id === tripId);
    let ticket = null;

    if (!tripLayer) {
        ticket = currentTickets.find(t => t.id === tripId);
        if (ticket) {
            tripLayer = tripLayers.find(item => 
                item.ticket.trainNo === ticket.trainNo &&
                item.ticket.departStation === ticket.departStation &&
                item.ticket.arriveStation === ticket.arriveStation
            );
        }
    } else {
        ticket = currentTickets.find(t => t.id === tripId) || tripLayer.ticket;
    }

    if (!tripLayer || !ticket) {
        return;
    }

    highlightTrips([tripLayer.id], true);

    const isCloseEnough = map.getZoom() >= 8;
    const departLatLng = L.latLng(ticket.departLat, ticket.departLng);
    const arriveLatLng = L.latLng(ticket.arriveLat, ticket.arriveLng);
    const currentBounds = map.getBounds();
    const isDepartInView = currentBounds.contains(departLatLng);
    const isArriveInView = currentBounds.contains(arriveLatLng);
    const isTripInView = isDepartInView && isArriveInView;

    if (isCloseEnough && isTripInView) {
        showTripInfoCard(tripId);
    } else {
        setTimeout(() => {
            const latlngs = [
                [ticket.departLat, ticket.departLng],
                [ticket.arriveLat, ticket.arriveLng]
            ];
            const bounds = L.latLngBounds(latlngs);
            map.flyToBounds(bounds.pad(0.3), {
                duration: 1.5,
                easeLinearity: 0.25
            });

            setTimeout(() => {
                showTripInfoCard(tripId);
            }, 1600);
        }, 100);
    }
}

// 调整视野显示选中的行程
function fitHighlightedTrips(highlightedItems) {
    if (highlightedItems.length === 0) return;

    const bounds = L.latLngBounds([]);
    highlightedItems.forEach(item => {
        const ticket = item.ticket;
        bounds.extend([ticket.departLat, ticket.departLng]);
        bounds.extend([ticket.arriveLat, ticket.arriveLng]);
    });

    map.flyToBounds(bounds.pad(0.3), {
        duration: 1.5,
        easeLinearity: 0.25
    });

    highlightedItems.forEach((item, index) => {
        const ticket = item.ticket;
        setTimeout(() => {
            addTripPulseMarkers(ticket.departLat, ticket.departLng, ticket.departStation, 
                               ticket.arriveLat, ticket.arriveLng, ticket.arriveStation);
        }, 1600 + (index * 200));
    });
}

// 添加脉冲动画标记
let pulseMarkers = [];

function addPulseMarker(lat, lng, title, type) {
    const color = type === 'start' ? '#4CAF50' : '#F44336';

    const pulseCircle = L.circleMarker([lat, lng], {
        radius: 15,
        fillColor: color,
        color: color,
        weight: 2,
        opacity: 0.8,
        fillOpacity: 0.3
    }).addTo(map);

    const centerDot = L.circleMarker([lat, lng], {
        radius: 6,
        fillColor: color,
        color: '#fff',
        weight: 2,
        opacity: 1,
        fillOpacity: 1
    }).addTo(map).bindPopup(title, {autoClose: false});

    let radius = 15;
    let opacity = 0.8;
    let growing = false;

    const animate = () => {
        if (growing) {
            radius += 0.5;
            opacity += 0.02;
            if (radius >= 25) growing = false;
        } else {
            radius -= 0.5;
            opacity -= 0.02;
            if (radius <= 15) growing = true;
        }

        pulseCircle.setStyle({
            radius: radius,
            opacity: Math.max(0.3, opacity),
            fillOpacity: Math.max(0.1, opacity * 0.3)
        });
    };

    const animationId = setInterval(animate, 50);

    pulseMarkers.push({
        pulseCircle: pulseCircle,
        centerDot: centerDot,
        animationId: animationId
    });
}

// 添加行程的脉冲标记
function addTripPulseMarkers(departLat, departLng, departStation, arriveLat, arriveLng, arriveStation) {
    clearPulseMarkers();
    
    addPulseMarker(departLat, departLng, departStation, 'start');
    addPulseMarker(arriveLat, arriveLng, arriveStation, 'end');
    
    setTimeout(() => {
        clearPulseMarkers();
    }, 5000);
}

// 清除脉冲标记
function clearPulseMarkers() {
    pulseMarkers.forEach(marker => {
        clearInterval(marker.animationId);
        map.removeLayer(marker.pulseCircle);
        map.removeLayer(marker.centerDot);
    });
    pulseMarkers = [];
}

// 调整视野显示所有行程
function fitAllTrips() {
    if (tripLayers.length === 0) return;

    const group = new L.featureGroup(tripLayers.map(item => item.layer));
    map.fitBounds(group.getBounds().pad(0.1));
}

// 清除所有图层
function clearLayers() {
    tripLayers.forEach(item => {
        map.removeLayer(item.layer);
        if (item.dateLabelMarker) {
            map.removeLayer(item.dateLabelMarker);
        }
    });
    tripLayers = [];

    stationMarkers.forEach(item => {
        map.removeLayer(item.marker);
        map.removeLayer(item.labelMarker);
    });
    stationMarkers = [];

    clearClusterMarkers();
}

// 清除聚合标记
function clearClusterMarkers() {
    clusterMarkers.forEach(marker => {
        map.removeLayer(marker);
    });
    clusterMarkers = [];
}

// 发送消息到WPF
function sendStationClick(stationName) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'stationClick',
            data: {stationName: stationName}
        }));
    }
}

function sendTripClick(tripId) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'tripClick',
            data: {tripId: tripId}
        }));
    }
}

function sendError(message) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'error',
            data: {message: message}
        }));
    }
}

function sendOpenTicketEdit(tripId) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'openTicketEdit',
            data: {tripId: tripId}
        }));
    }
}

function sendShowTicketDetails(tripId) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'showTicketDetails',
            data: {tripId: tripId}
        }));
    }
}

function sendShowStationTickets(stationName) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'showStationTickets',
            data: {stationName: stationName}
        }));
    }
}

function sendPreviewTicket(tripId) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'previewTicket',
            data: {tripId: tripId}
        }));
    }
}

function sendShowStationStats(stationName) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'showStationStats',
            data: {stationName: stationName}
        }));
    }
}

function sendCreateTicketFromStation(stationName) {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'createTicketFromStation',
            data: {stationName: stationName}
        }));
    }
}

function sendCreateNewTicket() {
    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'createNewTicket',
            data: {}
        }));
    }
}

// 清空选中状态
function clearSelection() {
    tripLayers.forEach(item => {
        const color = getTripColor(item.ticket.status);
        const weight = getTripWeight(item.ticket.status);
        item.layer.setStyle({
            color: color,
            weight: weight,
            opacity: 0.8
        });
    });

    clearPulseMarkers();

    if (window.chrome?.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'clearSelection',
            data: {}
        }));
    }
}

// 切换时间轴抽屉
function toggleTimelineDrawer() {
    const drawer = document.getElementById('timelineContainer');
    drawer.classList.toggle('expanded');
}

// 初始化时间轴
function initTimeline(tickets) {
    const years = [...new Set(tickets.map(t => {
        let year = null;

        const yearMatch = t.departDate.match(/^\d{4}/);
        if (yearMatch) {
            year = parseInt(yearMatch[0], 10);
        }

        if (!year || isNaN(year)) {
            const date = new Date(t.departDate);
            if (!isNaN(date.getTime())) {
                year = date.getFullYear();
            }
        }

        if (!year || isNaN(year)) {
            year = new Date().getFullYear();
        }

        return year;
    }))].sort((a, b) => a - b);

    if (years.length === 0) {
        document.getElementById('timelineContainer').style.display = 'none';
        return;
    }

    timelineYears = years;
    const slider = document.getElementById('timelineSlider');
    const labelsContainer = document.getElementById('timelineLabels');

    slider.min = 0;
    slider.max = years.length;
    slider.value = 0;

    labelsContainer.innerHTML = '';
    const allLabel = document.createElement('span');
    allLabel.textContent = '全部';
    labelsContainer.appendChild(allLabel);

    years.forEach(year => {
        const label = document.createElement('span');
        label.textContent = year;
        labelsContainer.appendChild(label);
    });

    document.getElementById('timelineContainer').style.display = 'block';

    slider.oninput = function () {
        const index = parseInt(this.value);
        if (index === 0) {
            currentYearFilter = null;
            document.getElementById('timelineCurrent').textContent = '全部年份';
        } else {
            currentYearFilter = timelineYears[index - 1];
            document.getElementById('timelineCurrent').textContent = currentYearFilter + '年';
        }
        applyYearFilter();
    };
}

// 安全解析年份
function safeGetYear(dateStr) {
    let year = null;

    const yearMatch = dateStr.match(/^\d{4}/);
    if (yearMatch) {
        year = parseInt(yearMatch[0], 10);
    }

    if (!year || isNaN(year)) {
        const date = new Date(dateStr);
        if (!isNaN(date.getTime())) {
            year = date.getFullYear();
        }
    }

    if (!year || isNaN(year)) {
        year = new Date().getFullYear();
    }

    return year;
}

// 应用年份筛选
function applyYearFilter() {
    const filter = currentTheme.directionFilter;
    
    tripLayers.forEach(item => {
        const ticketYear = safeGetYear(item.ticket.departDate);
        const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
        
        // 同时检查方向过滤条件
        let directionMatch = true;
        if (filter !== 'All') {
            directionMatch = shouldShowLayerForFilter(item, filter);
        }
        
        const shouldShow = yearMatch && directionMatch;

        if (shouldShow) {
            item.layer.addTo(map);
            if (item.dateLabelMarker) {
                item.dateLabelMarker.addTo(map);
            }
        } else {
            map.removeLayer(item.layer);
            if (item.dateLabelMarker) {
                map.removeLayer(item.dateLabelMarker);
            }
        }
    });

    updateStationsVisibility();
    updateClusters();
}

// 更新车站可见性
function updateStationsVisibility() {
    const visibleStations = new Set();
    const filter = currentTheme.directionFilter;
    
    tripLayers.forEach(item => {
        const ticketYear = safeGetYear(item.ticket.departDate);
        const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
        
        // 同时检查方向过滤条件
        let directionMatch = true;
        if (filter !== 'All') {
            directionMatch = shouldShowLayerForFilter(item, filter);
        }
        
        if (yearMatch && directionMatch) {
            visibleStations.add(item.ticket.departStation);
            visibleStations.add(item.ticket.arriveStation);
        }
    });

    stationMarkers.forEach(item => {
        if (visibleStations.has(item.name)) {
            item.marker.addTo(map);
            // 只有在设置显示车站标签时才显示
            if (currentTheme.showStationLabels) {
                item.labelMarker.addTo(map);
            } else {
                map.removeLayer(item.labelMarker);
            }
        } else {
            map.removeLayer(item.marker);
            map.removeLayer(item.labelMarker);
        }
    });
}

// 重置时间轴
function resetTimeline() {
    const slider = document.getElementById('timelineSlider');
    slider.value = 0;
    currentYearFilter = null;
    document.getElementById('timelineCurrent').textContent = '全部年份';
    applyYearFilter();
}

// 创建聚合标记
function createClusterMarker(trips, centerLat, centerLng) {
    const count = trips.length;
    const sizeClass = count < 5 ? 'small' : count < 15 ? 'medium' : 'large';

    const icon = L.divIcon({
        className: 'cluster-marker ' + sizeClass,
        html: `<div style="width:100%;height:100%;display:flex;align-items:center;justify-content:center;">${count}</div>`,
        iconSize: null
    });

    const marker = L.marker([centerLat, centerLng], {icon: icon});

    marker.on('click', function () {
        const bounds = L.latLngBounds();
        trips.forEach(trip => {
            bounds.extend([trip.departLat, trip.departLng]);
            bounds.extend([trip.arriveLat, trip.arriveLng]);
        });
        map.fitBounds(bounds.pad(0.2));
    });

    const statusCounts = {};
    trips.forEach(t => {
        statusCounts[t.status] = (statusCounts[t.status] || 0) + 1;
    });
    let tooltipContent = `<strong>${count} 条行程</strong><br/>`;
    for (const [status, cnt] of Object.entries(statusCounts)) {
        tooltipContent += `${status}: ${cnt}条<br/>`;
    }
    marker.bindTooltip(tooltipContent, {
        direction: 'top',
        offset: [0, -10],
        className: 'trip-hover-card'
    });

    return marker;
}

// 执行聚合
function clusterTrips(trips) {
    if (!clusterEnabled || trips.length < 10) {
        return null;
    }

    const zoom = map.getZoom();
    if (zoom >= clusterZoomThreshold) {
        return null;
    }

    const gridSize = 2.0;
    const clusters = new Map();

    trips.forEach(trip => {
        const centerLat = (trip.departLat + trip.arriveLat) / 2;
        const centerLng = (trip.departLng + trip.arriveLng) / 2;

        const gridX = Math.floor(centerLng / gridSize);
        const gridY = Math.floor(centerLat / gridSize);
        const key = `${gridX},${gridY}`;

        if (!clusters.has(key)) {
            clusters.set(key, {
                trips: [],
                totalLat: 0,
                totalLng: 0
            });
        }

        const cluster = clusters.get(key);
        cluster.trips.push(trip);
        cluster.totalLat += centerLat;
        cluster.totalLng += centerLng;
    });

    const result = [];
    clusters.forEach((cluster, key) => {
        if (cluster.trips.length >= 2) {
            const avgLat = cluster.totalLat / cluster.trips.length;
            const avgLng = cluster.totalLng / cluster.trips.length;
            result.push({
                marker: createClusterMarker(cluster.trips, avgLat, avgLng),
                trips: cluster.trips
            });
        }
    });

    return result;
}

// 更新聚合显示
function updateClusters() {
    clearClusterMarkers();

    const filter = currentTheme.directionFilter;
    const visibleTrips = [];
    tripLayers.forEach(item => {
        const ticketYear = safeGetYear(item.ticket.departDate);
        const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
        
        // 同时检查方向过滤条件
        let directionMatch = true;
        if (filter !== 'All') {
            directionMatch = shouldShowLayerForFilter(item, filter);
        }
        
        if (yearMatch && directionMatch) {
            visibleTrips.push(item.ticket);
        }
    });

    const clusters = clusterTrips(visibleTrips);

    if (clusters && clusters.length > 0) {
        const clusteredTripIds = new Set();
        clusters.forEach(c => {
            c.trips.forEach(t => clusteredTripIds.add(t.id));
        });

        tripLayers.forEach(item => {
            if (clusteredTripIds.has(item.ticket.id)) {
                map.removeLayer(item.layer);
                if (item.dateLabelMarker) {
                    map.removeLayer(item.dateLabelMarker);
                }
            }
        });

        clusters.forEach(c => {
            c.marker.addTo(map);
            clusterMarkers.push(c.marker);
        });
    }
}

// 监听地图缩放事件
function setupClusterListener() {
    map.on('zoomend', function () {
        const zoom = map.getZoom();
        const filter = currentTheme.directionFilter;
        
        if (currentTheme.showDateLabels && dateLabelData.length > 0) {
            if (zoom <= 13) {
                updateAllDateLabels();
            } else {
                tripLayers.forEach(item => {
                    if (item.dateLabelMarker) {
                        map.removeLayer(item.dateLabelMarker);
                    }
                });
            }
        }

        if (currentTickets.length > 0) {
            clearClusterMarkers();

            tripLayers.forEach(item => {
                const ticketYear = safeGetYear(item.ticket.departDate);
                const yearMatch = currentYearFilter === null || ticketYear === currentYearFilter;
                
                // 同时检查方向过滤条件
                let directionMatch = true;
                if (filter !== 'All') {
                    directionMatch = shouldShowLayerForFilter(item, filter);
                }
                
                const shouldShow = yearMatch && directionMatch;

                if (shouldShow) {
                    item.layer.addTo(map);
                    if (zoom <= 13 && item.dateLabelMarker) {
                        item.dateLabelMarker.addTo(map);
                    }
                }
            });

            updateClusters();
            
            // 更新车站可见性（包括标签）
            updateStationsVisibility();
        }
    });
}

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function () {
    initMap();
    setupClusterListener();

    if (currentTheme.isDarkMode) {
        document.body.classList.add('dark-mode');
    } else {
        document.body.classList.remove('dark-mode');
    }
});
