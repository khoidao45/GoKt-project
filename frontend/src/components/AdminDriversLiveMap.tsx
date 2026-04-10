import { useEffect, useMemo, useRef } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import type { AdminDriverDto } from '../types'

interface Props {
  drivers: AdminDriverDto[]
}

const DEFAULT_CENTER: [number, number] = [10.7769, 106.7009]

export default function AdminDriversLiveMap({ drivers }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const markersLayerRef = useRef<L.LayerGroup | null>(null)

  const visibleDrivers = useMemo(() => (
    drivers.filter(d => typeof d.latitude === 'number' && typeof d.longitude === 'number')
  ), [drivers])

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

    mapRef.current = map
    markersLayerRef.current = layer

    return () => {
      map.remove()
      mapRef.current = null
      markersLayerRef.current = null
    }
  }, [])

  useEffect(() => {
    const map = mapRef.current
    const layer = markersLayerRef.current
    if (!map || !layer) return

    layer.clearLayers()

    if (visibleDrivers.length === 0) {
      map.setView(DEFAULT_CENTER, 12)
      return
    }

    const bounds = L.latLngBounds([])

    for (const driver of visibleDrivers) {
      const lat = driver.latitude as number
      const lng = driver.longitude as number
      const color = driver.isBusy ? '#f59e0b' : '#10b981'

      const circle = L.circleMarker([lat, lng], {
        radius: 8,
        color,
        fillColor: color,
        fillOpacity: 0.9,
        weight: 2,
      })

      circle.bindPopup(
        `<div style="min-width:220px;font-size:13px;line-height:1.45">` +
          `<div style="font-weight:700;margin-bottom:6px">${driver.fullName || 'Tai xe'}</div>` +
          `<div><strong>Ma:</strong> ${driver.driverCode || '-'}</div>` +
          `<div><strong>Online:</strong> ${driver.isOnline ? 'Co' : 'Khong'}</div>` +
          `<div><strong>Ban:</strong> ${driver.isBusy ? 'Dang co cuoc' : 'Ranh'}</div>` +
          `<div><strong>Rating:</strong> ${driver.rating.toFixed(1)} star</div>` +
          `<div><strong>Vi tri:</strong> ${lat.toFixed(5)}, ${lng.toFixed(5)}</div>` +
        `</div>`,
      )

      circle.addTo(layer)
      bounds.extend([lat, lng])
    }

    map.fitBounds(bounds, { padding: [36, 36], maxZoom: 15 })
  }, [visibleDrivers])

  return (
    <div
      ref={containerRef}
      style={{
        height: 520,
        width: '100%',
        border: '1px solid var(--border)',
        borderRadius: 12,
        overflow: 'hidden',
      }}
    />
  )
}
