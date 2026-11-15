"use client";

import { useEffect, useRef, type ReactNode } from "react";

type TiltFrameProps = {
  children: ReactNode;
  className?: string;
  /** Largest tilt in degrees when the pointer is at the edge. */
  maxTilt?: number;
  /** Pixels the frame drifts up for each 100 px scrolled (parallax). */
  parallax?: number;
};

/**
 * A gentle 3D tilt that follows the mouse, plus a slow scroll parallax.
 * Off for touch screens and for visitors who ask for reduced motion: the
 * child is then shown flat and still.
 */
export function TiltFrame({ children, className = "", maxTilt = 5, parallax = 6 }: TiltFrameProps) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)");
    const finePointer = window.matchMedia("(hover: hover) and (pointer: fine)");
    if (reduce.matches) return;

    let frame = 0;
    const set = (name: string, value: string) => element.style.setProperty(name, value);

    const onScroll = () => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(() => {
        const rect = element.getBoundingClientRect();
        const fromCentre = rect.top + rect.height / 2 - window.innerHeight / 2;
        const offset = Math.max(-40, Math.min(40, (-fromCentre * parallax) / 100));
        set("--py", `${offset.toFixed(1)}px`);
      });
    };

    const onMove = (event: PointerEvent) => {
      const rect = element.getBoundingClientRect();
      const x = (event.clientX - rect.left) / rect.width - 0.5;
      const y = (event.clientY - rect.top) / rect.height - 0.5;
      set("--ry", `${(x * maxTilt * 2).toFixed(2)}deg`);
      set("--rx", `${(-y * maxTilt * 2).toFixed(2)}deg`);
    };

    const onLeave = () => {
      set("--ry", "0deg");
      set("--rx", "0deg");
    };

    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();
    if (finePointer.matches) {
      element.addEventListener("pointermove", onMove);
      element.addEventListener("pointerleave", onLeave);
    }
    return () => {
      cancelAnimationFrame(frame);
      window.removeEventListener("scroll", onScroll);
      element.removeEventListener("pointermove", onMove);
      element.removeEventListener("pointerleave", onLeave);
    };
  }, [maxTilt, parallax]);

  return (
    <div ref={ref} className={`tilt ${className}`}>
      {children}
    </div>
  );
}
