import { useEffect, useRef } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'

interface Point {
  lat: number
  lng: number
}

interface Props {
  riderPos: Point | null
  driverPos: Point | null
  pickupPos?: Point | null
  dropoffPos?: Point | null
  height?: number
}

const DEFAULT_CENTER: [number, number] = [10.7769, 106.7009]

function isValidPoint(value: Point | null | undefined): value is Point {
  if (!value) return false
  return Number.isFinite(value.lat) && Number.isFinite(value.lng)
}

function isSamePoint(a: Point | null, b: Point | null, epsilon = 0.00001): boolean {
  if (!a || !b) return false
  return Math.abs(a.lat - b.lat) <= epsilon && Math.abs(a.lng - b.lng) <= epsilon
}

export default function TripLiveMap({ riderPos, driverPos, pickupPos, dropoffPos, height = 260 }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const layerRef = useRef<L.LayerGroup | null>(null)
  const routeLayerRef = useRef<L.LayerGroup | null>(null)

  const getRoute = async (from: Point, to: Point): Promise<[number, number][] | null> => {
    try {
      const url = `https://router.project-osrm.org/route/v1/driving/${from.lng},${from.lat};${to.lng},${to.lat}?overview=full&geometries=geojson`
      const res = await fetch(url)
      if (!res.ok) return null
      const data = await res.json()
      const coords: [number, number][] | undefined = data?.routes?.[0]?.geometry?.coordinates
      if (!coords || coords.length === 0) return null
      return coords.map((c: [number, number]) => [c[1], c[0]])
    } catch {
      return null
    }
  }

  useEffect(() => {
    if (!containerRef.current || mapRef.current) return

    const map = L.map(containerRef.current, {
      center: DEFAULT_CENTER,
      zoom: 13,
      zoomControl: true,
    })

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(map)

    const layer = L.layerGroup().addTo(map)
    const routeLayer = L.layerGroup().addTo(map)
    mapRef.current = map
    layerRef.current = layer
    routeLayerRef.current = routeLayer

    return () => {
      map.remove()
      mapRef.current = null
      layerRef.current = null
      routeLayerRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    const layer = layerRef.current
    const routeLayer = routeLayerRef.current
    if (!map || !layer || !routeLayer) return

    layer.clearLayers()
    routeLayer.clearLayers()

    const points: L.LatLngExpression[] = []

    const rider = isValidPoint(riderPos) ? riderPos : null
    const driver = isValidPoint(driverPos) ? driverPos : null
    const pickup = isValidPoint(pickupPos) ? pickupPos : rider
    const dropoff = isValidPoint(dropoffPos) ? dropoffPos : null
    const riderFallback = !rider && pickup ? pickup : null

    if (rider) {
      const riderMarker = L.circleMarker([rider.lat, rider.lng], {
        radius: 9,
        color: '#ffffff',
        fillColor: '#22c55e',
        fillOpacity: 1,
        weight: 2.5,
      })
        .bindPopup('Khach hang')
        .bindTooltip('Khach hang', { permanent: true, direction: 'top', offset: [0, -10], opacity: 0.95 })
      riderMarker.addTo(layer)
      points.push([rider.lat, rider.lng])
    }

    if (riderFallback) {
      const riderFallbackMarker = L.circleMarker([riderFallback.lat, riderFallback.lng], {
        radius: 8,
        color: '#ffffff',
        fillColor: '#16a34a',
        fillOpacity: 0.78,
        weight: 2,
      })
        .bindPopup('Khach hang (uoc tinh tai diem don)')
        .bindTooltip('Khach hang (uoc tinh)', { permanent: true, direction: 'top', offset: [0, -10], opacity: 0.95 })
      riderFallbackMarker.addTo(layer)
      points.push([riderFallback.lat, riderFallback.lng])
    }

    if (driver) {
      const driverMarker = L.circleMarker([driver.lat, driver.lng], {
        radius: 9,
        color: '#ffffff',
        fillColor: '#2563eb',
        fillOpacity: 1,
        weight: 2.5,
      })
        .bindPopup('Tai xe')
        .bindTooltip('Tai xe', { permanent: true, direction: 'top', offset: [0, -10], opacity: 0.95 })
      driverMarker.addTo(layer)
      points.push([driver.lat, driver.lng])
    }

    if (pickup && !isSamePoint(pickup, rider) && !isSamePoint(pickup, riderFallback)) {
      const pickupMarker = L.circleMarker([pickup.lat, pickup.lng], {
        radius: 7,
        color: '#ffffff',
        fillColor: '#f59e0b',
        fillOpacity: 0.9,
        weight: 2,
      }).bindPopup('Diem don')
      pickupMarker.addTo(layer)
      points.push([pickup.lat, pickup.lng])
    }

    if (dropoff) {
      const dropoffMarker = L.circleMarker([dropoff.lat, dropoff.lng], {
        radius: 7,
        color: '#ef4444',
        fillColor: '#ef4444',
        fillOpacity: 0.9,
        weight: 2,
      }).bindPopup('Diem tra')
      dropoffMarker.addTo(layer)
      points.push([dropoff.lat, dropoff.lng])
    }

    const drawRoutes = async () => {
      if (driver && pickup) {
        const routeToPickup = await getRoute(driver, pickup)
        if (routeToPickup && routeToPickup.length > 1) {
          L.polyline(routeToPickup, {
            color: '#f59e0b',
            weight: 4,
            opacity: 0.85,
            dashArray: '8 6',
          }).addTo(routeLayer)
        }
      }

      if (pickup && dropoff) {
        const routeToDropoff = await getRoute(pickup, dropoff)
        if (routeToDropoff && routeToDropoff.length > 1) {
          L.polyline(routeToDropoff, {
            color: '#3b82f6',
            weight: 5,
            opacity: 0.9,
          }).addTo(routeLayer)
        }
      }
    }

    void drawRoutes()

    if (points.length === 0) {
      map.setView(DEFAULT_CENTER, 12)
      return
    }

    if (points.length === 1) {
      map.setView(points[0] as [number, number], 15)
      return
    }

    const bounds = L.latLngBounds(points)
    map.fitBounds(bounds, { padding: [28, 28], maxZoom: 16 })
  }, [riderPos, driverPos, pickupPos, dropoffPos])

  return (
    <div style={{ border: '1px solid #334155', borderRadius: 10, overflow: 'hidden' }}>
      <div ref={containerRef} style={{ height, width: '100%' }} />
      <div style={{ display: 'flex', gap: 14, padding: '10px 12px', fontSize: 13, color: '#e2e8f0', background: '#0f172a', fontWeight: 600 }}>
        <span><strong style={{ color: '#22c55e' }}>●</strong> Khach hang</span>
        <span><strong style={{ color: '#2563eb' }}>●</strong> Tai xe</span>
        <span><strong style={{ color: '#f59e0b' }}>●</strong> Diem don</span>
        <span><strong style={{ color: '#ef4444' }}>●</strong> Diem tra</span>
        <span><strong style={{ color: '#f59e0b' }}>---</strong> Den diem don</span>
        <span><strong style={{ color: '#3b82f6' }}>---</strong> Den diem tra</span>
      </div>
    </div>
  )
}
