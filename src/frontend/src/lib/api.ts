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

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

export async function convertMeasure(
  payload: ConvertMeasureRequest,
): Promise<ConvertMeasureResponse> {
  const response = await fetch(`${API_BASE}/units/convert`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const detail = await response.text();
    throw new Error(`Conversion failed (${response.status}): ${detail}`);
  }

  return response.json();
}
