export class ApiError extends Error {
  readonly status: number;
  readonly title: string | null;
  readonly detail: string | null;
  readonly code: string | null;

  constructor(
    status: number,
    message: string,
    title: string | null = null,
    detail: string | null = null,
    code: string | null = null,
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.code = code;
  }
}

export interface HttpRequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  json?: unknown;
  token?: string | null;
  idempotent?: boolean;
  idempotencyKey?: string;
}

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

/**
 * Generates a stable idempotency key. Uses a caller-supplied key when provided,
 * otherwise produces a random UUID. Kept dependency-free for unit testing.
 */
export function generateIdempotencyKey(prefix?: string): string {
  const uuid =
    typeof crypto !== 'undefined' && 'randomUUID' in crypto
      ? crypto.randomUUID()
      : `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;

  return prefix ? `${prefix}:${uuid}` : uuid;
}

export interface ParsedProblem {
  title: string | null;
  detail: string | null;
  code: string | null;
}

export function parseProblem(detail: unknown): ParsedProblem {
  if (!detail || typeof detail !== 'object') {
    return { title: null, detail: null, code: null };
  }

  const obj = detail as Record<string, unknown>;
  return {
    title: typeof obj['title'] === 'string' ? obj['title'] : null,
    detail: typeof obj['detail'] === 'string' ? obj['detail'] : null,
    code: typeof obj['code'] === 'string' ? obj['code'] : null,
  };
}

export async function httpRequest<T>(path: string, options: HttpRequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {};

  if (options.json !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  if (options.token) {
    headers['Authorization'] = `Bearer ${options.token}`;
  }

  if (options.idempotent) {
    headers['Idempotency-Key'] = options.idempotencyKey ?? generateIdempotencyKey();
  }

  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.json !== undefined ? JSON.stringify(options.json) : undefined,
  });

  if (response.ok) {
    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }

  let problem: ParsedProblem = { title: null, detail: null, code: null };
  try {
    problem = parseProblem(await response.json());
  } catch {
    // Non-JSON error body; fall back to an empty problem.
  }

  const message =
    problem.detail ?? problem.title ?? `Request failed with status ${response.status}`;

  throw new ApiError(response.status, message, problem.title, problem.detail, problem.code);
}
