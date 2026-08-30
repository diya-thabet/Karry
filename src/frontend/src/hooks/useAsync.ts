import { useCallback, useEffect, useRef, useState } from 'react';

export interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

/**
 * Runs an async loader on mount (and re-runs whenever `deps` change), exposing
 * loading / error / data plus an imperative `reload`. Safe against setState after
 * unmount and stale closures via a request-generation counter.
 */
export function useAsync<T>(loader: () => Promise<T>, deps: readonly unknown[] = []) {
  const [state, setState] = useState<AsyncState<T>>({ data: null, loading: true, error: null });
  const generation = useRef(0);
  const loaderRef = useRef(loader);

  useEffect(() => {
    loaderRef.current = loader;
  }, [loader]);

  const run = useCallback(async () => {
    const gen = ++generation.current;
    setState((s) => ({ ...s, loading: true, error: null }));

    try {
      const data = await loaderRef.current();
      if (gen === generation.current) {
        setState({ data, loading: false, error: null });
      }
    } catch (error) {
      if (gen === generation.current) {
        setState({
          data: null,
          loading: false,
          error: error instanceof Error ? error.message : 'Request failed.',
        });
      }
    }
  }, []);

  useEffect(() => {
    void run();
    return () => {
      generation.current += 1;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return { ...state, reload: run };
}
