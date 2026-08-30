import { UnitToggle } from '@/features/units/UnitToggle';

export function HomePage() {
  return (
    <section className="mx-auto max-w-3xl">
      <h2 className="mb-4 text-2xl font-semibold text-slate-800">Dashboard</h2>
      <p className="mb-8 text-slate-600">
        Field extraction, plant processing and executive intelligence will surface here as modules
        are built.
      </p>
      <UnitToggle />
    </section>
  );
}
