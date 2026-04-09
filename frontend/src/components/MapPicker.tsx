import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'

// Fix Leaflet default icon paths broken by Vite bundling
delete (L.Icon.Default.prototype as unknown as Record<string, unknown>)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
})

const pickupIcon = L.divIcon({
  html: `<div style="background:#10b981;width:16px;height:16px;border-radius:50%;border:3px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.4)"></div>`,
  className: '',
  iconSize: [16, 16],
  iconAnchor: [8, 8],
})

const dropoffIcon = L.divIcon({
  html: `<div style="background:#ef4444;width:16px;height:16px;border-radius:50%;border:3px solid #fff;box-shadow:0 2px 6px rgba(0,0,0,.4)"></div>`,
  className: '',
  iconSize: [16, 16],
  iconAnchor: [8, 8],
})

export interface LatLng { lat: number; lng: number }

interface Props {
  pickup: LatLng | null
  dropoff: LatLng | null
  onPickupChange: (pos: LatLng, address: string) => void
  onDropoffChange: (pos: LatLng, address: string) => void
  /** 'pickup' = next click sets pickup, 'dropoff' = next click sets dropoff */
  mode: 'pickup' | 'dropoff'
  height?: number
}

async function reverseGeocode(lat: number, lng: number): Promise<string> {
  try {
    const res = await fetch(
      `https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json`,
      { headers: { 'Accept-Language': 'vi' } },
    )
    const data = await res.json()
    return data.display_name ?? `${lat.toFixed(5)}, ${lng.toFixed(5)}`
  } catch {
    return `${lat.toFixed(5)}, ${lng.toFixed(5)}`
  }
}

export default function MapPicker({ pickup, dropoff, onPickupChange, onDropoffChange, mode, height = 320 }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const pickupMarkerRef = useRef<L.Marker | null>(null)
  const dropoffMarkerRef = useRef<L.Marker | null>(null)
  const [geocoding, setGeocoding] = useState(false)

  // Init map once
  useEffect(() => {
    if (!containerRef.current || mapRef.current) return

    const map = L.map(containerRef.current, {
      center: [10.7769, 106.7009], // Ho Chi Minh City
      zoom: 14,
      zoomControl: true,
    })

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(map)

    mapRef.current = map

    return () => {
      map.remove()
      mapRef.current = null
    }
  }, [])

  // Handle map click → place marker
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    const onClick = async (e: L.LeafletMouseEvent) => {
      const { lat, lng } = e.latlng
      setGeocoding(true)
      const address = await reverseGeocode(lat, lng)
      setGeocoding(false)

      if (mode === 'pickup') {
        onPickupChange({ lat, lng }, address)
      } else {
        onDropoffChange({ lat, lng }, address)
      }
    }

    map.on('click', onClick)
    return () => { map.off('click', onClick) }
  }, [mode, onPickupChange, onDropoffChange])

  // Sync pickup marker
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    if (pickup) {
      if (pickupMarkerRef.current) {
        pickupMarkerRef.current.setLatLng([pickup.lat, pickup.lng])
      } else {
        pickupMarkerRef.current = L.marker([pickup.lat, pickup.lng], { icon: pickupIcon })
          .addTo(map)
          .bindPopup('📍 Điểm đón')
      }
    } else if (pickupMarkerRef.current) {
      pickupMarkerRef.current.remove()
      pickupMarkerRef.current = null
    }
  }, [pickup])

  // Sync dropoff marker
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    if (dropoff) {
      if (dropoffMarkerRef.current) {
        dropoffMarkerRef.current.setLatLng([dropoff.lat, dropoff.lng])
      } else {
        dropoffMarkerRef.current = L.marker([dropoff.lat, dropoff.lng], { icon: dropoffIcon })
          .addTo(map)
          .bindPopup('🏁 Điểm đến')
      }
    } else if (dropoffMarkerRef.current) {
      dropoffMarkerRef.current.remove()
      dropoffMarkerRef.current = null
    }
  }, [dropoff])

  // Fit bounds when both points set
  useEffect(() => {
    const map = mapRef.current
    if (!map || !pickup || !dropoff) return
    map.fitBounds([[pickup.lat, pickup.lng], [dropoff.lat, dropoff.lng]], { padding: [48, 48] })
  }, [pickup, dropoff])

  return (
    <div style={{ position: 'relative' }}>
      <div ref={containerRef} style={{ height, borderRadius: 12, overflow: 'hidden', border: '1.5px solid var(--border)' }} />

      {/* Legend overlay */}
      <div style={{
        position: 'absolute', top: 10, left: 10, zIndex: 1000,
        background: 'rgba(255,255,255,.92)', backdropFilter: 'blur(4px)',
        borderRadius: 8, padding: '8px 12px', fontSize: 12,
        boxShadow: '0 2px 8px rgba(0,0,0,.12)',
        display: 'flex', flexDirection: 'column', gap: 4,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <div style={{ width: 10, height: 10, borderRadius: '50%', background: '#10b981' }} />
          <span>Điểm đón</span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
          <div style={{ width: 10, height: 10, borderRadius: '50%', background: '#ef4444' }} />
          <span>Điểm đến</span>
        </div>
      </div>

      {/* Active mode indicator */}
      <div style={{
        position: 'absolute', bottom: 10, left: '50%', transform: 'translateX(-50%)',
        zIndex: 1000, background: mode === 'pickup' ? '#10b981' : '#ef4444',
        color: '#fff', borderRadius: 999, padding: '5px 14px', fontSize: 12, fontWeight: 600,
        boxShadow: '0 2px 8px rgba(0,0,0,.2)',
        pointerEvents: 'none',
      }}>
        {geocoding ? '⏳ Đang lấy địa chỉ...' : mode === 'pickup' ? '👆 Nhấn chọn điểm đón' : '👆 Nhấn chọn điểm đến'}
      </div>
    </div>
  )
}
