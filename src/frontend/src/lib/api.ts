import { ApiError, httpRequest } from '@/lib/http';
import { getAccessToken } from '@/features/auth/tokenManager';

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

export async function convertMeasure(
  payload: ConvertMeasureRequest,
): Promise<ConvertMeasureResponse> {
  const token = await getAccessToken();

  return httpRequest<ConvertMeasureResponse>('/units/convert', {
    method: 'POST',
    json: payload,
    token,
    idempotent: true,
  });
}

export { ApiError };
