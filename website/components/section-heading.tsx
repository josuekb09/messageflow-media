type SectionHeadingProps = {
  title: string;
  lead?: string;
  align?: "left" | "center";
};

/** Section title with a short cyan rule under it, and an optional lead. */
export function SectionHeading({ title, lead, align = "left" }: SectionHeadingProps) {
  const centered = align === "center";
  return (
    <div className={centered ? "mx-auto max-w-2xl text-center" : "max-w-2xl"}>
      <h2 className="text-balance text-[2rem] font-semibold leading-[1.12] text-ink sm:text-[2.5rem]">
        {title}
      </h2>
      <span
        aria-hidden="true"
        className={`mt-5 block h-1 w-14 rounded-full bg-gradient-to-r from-accent to-accent/20 ${centered ? "mx-auto" : ""}`}
      />
      {lead ? (
        <p className="mt-5 text-pretty text-[16px] leading-7 text-ink-secondary sm:text-[17px] sm:leading-8">
          {lead}
        </p>
      ) : null}
    </div>
  );
}
