import { apiClient } from './client'
import type { FacilitySettingsResponse, SaveFacilitySettingsRequest } from '../types/facility'

export const getFacilitySettings = () =>
  apiClient.get<FacilitySettingsResponse>('/facility-settings').then((r) => r.data)

export const saveFacilitySettings = (data: SaveFacilitySettingsRequest) =>
  apiClient.put<FacilitySettingsResponse>('/facility-settings', data).then((r) => r.data)

export const uploadLogo = (file: File) => {
  const form = new FormData()
  form.append('file', file)
  return apiClient.post<FacilitySettingsResponse>('/facility-settings/logo', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then((r) => r.data)
}

export const deleteLogo = () =>
  apiClient.delete('/facility-settings/logo').then((r) => r.data)
