export function AnimatedBackground() {
  return (
    <div
      aria-hidden
      className="pointer-events-none fixed inset-0 -z-10 select-none overflow-hidden"
    >
      <div className="absolute inset-0 bg-[var(--rs-bg)]" />

      {/* Indigo orb */}
      <div
        className="absolute h-[700px] w-[700px] rounded-full opacity-[0.18] blur-[140px]"
        style={{
          background:
            "radial-gradient(circle, var(--rs-accent) 0%, transparent 70%)",
          top: "-15%",
          left: "5%",
          animation: "orb1 22s ease-in-out infinite"
        }}
      />

      {/* Violet orb */}
      <div
        className="absolute h-[500px] w-[500px] rounded-full opacity-[0.12] blur-[110px]"
        style={{
          background:
            "radial-gradient(circle, var(--rs-accent-violet) 0%, transparent 70%)",
          bottom: "5%",
          right: "3%",
          animation: "orb2 17s ease-in-out infinite"
        }}
      />

      {/* Cyan orb */}
      <div
        className="absolute h-[350px] w-[350px] rounded-full opacity-[0.08] blur-[90px]"
        style={{
          background:
            "radial-gradient(circle, var(--rs-chart-3) 0%, transparent 70%)",
          top: "45%",
          right: "28%",
          animation: "orb3 28s ease-in-out infinite"
        }}
      />

      {/* Subtle grid overlay */}
      <div
        className="absolute inset-0 opacity-[0.025]"
        style={{
          backgroundImage: `
            linear-gradient(rgba(255,255,255,0.6) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255,255,255,0.6) 1px, transparent 1px)
          `,
          backgroundSize: "64px 64px"
        }}
      />
    </div>
  );
}
