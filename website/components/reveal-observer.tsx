"use client";

import { usePathname } from "next/navigation";
import { useEffect } from "react";

/**
 * Shows every `.reveal` element as it scrolls into view by setting `data-shown`.
 * The hiding and the animation live in globals.css and only apply when motion is
 * allowed, so this component never hides anything on its own. One observer for
 * the whole page; it looks again after each navigation.
 */
export function RevealObserver() {
  const pathname = usePathname();

  useEffect(() => {
    const elements = Array.from(document.querySelectorAll<HTMLElement>(".reveal:not([data-shown])"));
    if (!("IntersectionObserver" in window)) {
      elements.forEach((element) => element.setAttribute("data-shown", ""));
      return;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.setAttribute("data-shown", "");
            observer.unobserve(entry.target);
          }
        }
      },
      { rootMargin: "0px 0px -8% 0px", threshold: 0 },
    );
    elements.forEach((element) => observer.observe(element));
    return () => observer.disconnect();
  }, [pathname]);

  return null;
}
