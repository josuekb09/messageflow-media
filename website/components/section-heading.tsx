type SectionHeadingProps = {
  title: string;
  lead?: string;
  align?: "left" | "center";
};

/** Serif section title with the thin gold rule under it, and an optional lead. */
export function SectionHeading({ title, lead, align = "left" }: SectionHeadingProps) {
  const centered = align === "center";
  return (
    <div className={centered ? "mx-auto max-w-2xl text-center" : "max-w-2xl"}>
      <h2 className="text-balance text-[2rem] font-medium leading-[1.15] text-ink sm:text-[2.6rem]">
        {title}
      </h2>
      <span
        aria-hidden="true"
        className={`mt-5 block h-px w-14 bg-brand ${centered ? "mx-auto" : ""}`}
      />
      {lead ? (
        <p className="mt-5 text-pretty text-[16px] leading-7 text-ink-secondary sm:text-[17px] sm:leading-8">
          {lead}
        </p>
      ) : null}
    </div>
  );
}
