import { httpRequest } from '@/lib/http';

export interface ConvertMeasureRequest {
  value: number;
  fromUnit: string;
  rhoDryTonPerM3: number;
  kappaMoisture: number;
}

export interface ConvertMeasureResponse {
  value: number;
  toUnit: string;
  appliedDensity: number;
  appliedMoistureFactor: number;
}

export interface SetUnitPreferencesRequest {
  massUnit?: string | null;
  volumeUnit?: string | null;
}

export async function convertMeasure(
  accessToken: string | null,
  payload: ConvertMeasureRequest,
): Promise<ConvertMeasureResponse> {
  return httpRequest<ConvertMeasureResponse>('/units/convert', {
    method: 'POST',
    json: payload,
    token: accessToken,
    idempotent: true,
  });
}

export function setUnitPreferences(
  accessToken: string,
  request: SetUnitPreferencesRequest,
): Promise<void> {
  return httpRequest<void>('/units/preferences', {
    method: 'PUT',
    json: request,
    token: accessToken,
    idempotent: true,
  });
}
