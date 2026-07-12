"use client";

import { useMemo, type ReactNode } from "react";
import { cn } from "@/shared/lib/cn";
import {
  formatBranchDateLabel,
  formatBranchTimeLabel,
  getBranchTimeParts
} from "@/shared/lib/date-time";

export type CalendarEventTone =
  | "accent"
  | "success"
  | "warning"
  | "danger"
  | "neutral";

export type CalendarEvent = {
  id: string;
  startUtc: string;
  endUtc: string;
  title: string;
  subtitle?: string;
  meta?: ReactNode;
  tone?: CalendarEventTone;
};

export type CalendarView = "day" | "week";

type CalendarGridProps = {
  events: ReadonlyArray<CalendarEvent>;
  view: CalendarView;
  branchTimeZoneId: string;
  /** Anchor UTC ISO for day view, or any date inside the target week for week view. */
  selectedDateUtc: string;
  workingHourStart?: number;
  workingHourEnd?: number;
  rowHeightPx?: number;
  onEventClick?: (event: CalendarEvent) => void;
  className?: string;
  emptyHint?: string;
};

const WEEKDAY_LABELS = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"] as const;

const toneStyles: Record<CalendarEventTone, string> = {
  accent: "border-[var(--rs-accent-strong)]/30 bg-[var(--rs-accent-soft)] text-[var(--rs-accent-strong)]",
  success: "border-[var(--rs-success)]/30 bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  warning: "border-[var(--rs-warning)]/30 bg-[var(--rs-warning-soft)] text-[var(--rs-warning)]",
  danger: "border-[var(--rs-danger)]/30 bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  neutral: "border-[var(--rs-border)] bg-[var(--rs-surface-muted)] text-[var(--rs-muted-strong)]"
};

function startOfWeekUtc(anchorUtc: string, branchTimeZoneId: string): string {
  const parts = getBranchTimeParts(anchorUtc, branchTimeZoneId);

  if (!parts) {
    return anchorUtc;
  }

  // weekday: Mon=1 .. Sun=0 → map to Mon-based offset
  const offset = parts.weekday === 0 ? 6 : parts.weekday - 1;
  const localMs = Date.UTC(parts.year, parts.month - 1, parts.day);
  const mondayMs = localMs - offset * 24 * 60 * 60 * 1000;
  return new Date(mondayMs).toISOString();
}

function buildDayColumn(args: {
  dayStartUtc: string;
  events: ReadonlyArray<CalendarEvent>;
  branchTimeZoneId: string;
}) {
  const { branchTimeZoneId, dayStartUtc, events } = args;
  const dayParts = getBranchTimeParts(dayStartUtc, branchTimeZoneId);

  if (!dayParts) {
    return [];
  }

  return events.filter((event) => {
    const eventParts = getBranchTimeParts(event.startUtc, branchTimeZoneId);
    return (
      eventParts !== null &&
      eventParts.year === dayParts.year &&
      eventParts.month === dayParts.month &&
      eventParts.day === dayParts.day
    );
  });
}

function computeEventLayout(args: {
  event: CalendarEvent;
  dayStartUtc: string;
  branchTimeZoneId: string;
  workingHourStart: number;
  workingHourEnd: number;
  rowHeightPx: number;
}) {
  const {
    branchTimeZoneId,
    dayStartUtc,
    event,
    rowHeightPx,
    workingHourEnd,
    workingHourStart
  } = args;
  const dayParts = getBranchTimeParts(dayStartUtc, branchTimeZoneId);

  if (!dayParts) {
    return null;
  }

  const start = getBranchTimeParts(event.startUtc, branchTimeZoneId);
  const end = getBranchTimeParts(event.endUtc, branchTimeZoneId);

  if (!start || !end) {
    return null;
  }

  if (
    start.year !== dayParts.year ||
    start.month !== dayParts.month ||
    start.day !== dayParts.day
  ) {
    return null;
  }

  const startOffsetHours = start.hour + start.minute / 60 - workingHourStart;
  const sameDayEnd =
    end.year === start.year &&
    end.month === start.month &&
    end.day === start.day;
  const endHour = sameDayEnd
    ? end.hour + end.minute / 60
    : workingHourEnd;
  const durationHours = Math.max(0.25, endHour - (startOffsetHours + workingHourStart));

  return {
    top: startOffsetHours * rowHeightPx,
    height: Math.max(rowHeightPx * 0.5, durationHours * rowHeightPx)
  };
}

export function CalendarGrid({
  branchTimeZoneId,
  className,
  emptyHint = "Bu görünümde planlanmış randevu yok.",
  events,
  onEventClick,
  rowHeightPx = 56,
  selectedDateUtc,
  view,
  workingHourEnd = 18,
  workingHourStart = 8
}: CalendarGridProps) {
  const hours = useMemo(() => {
    const list: number[] = [];
    for (let h = workingHourStart; h < workingHourEnd; h += 1) {
      list.push(h);
    }
    return list;
  }, [workingHourStart, workingHourEnd]);

  const columns = useMemo(() => {
    if (view === "day") {
      return [{ key: "day", label: formatBranchDateLabel(selectedDateUtc, branchTimeZoneId), dayStartUtc: selectedDateUtc }];
    }

    const mondayUtc = startOfWeekUtc(selectedDateUtc, branchTimeZoneId);
    return Array.from({ length: 7 }).map((_, index) => {
      const dayStartUtc = new Date(
        new Date(mondayUtc).getTime() + index * 24 * 60 * 60 * 1000
      ).toISOString();
      return {
        key: `day-${index}`,
        label: `${WEEKDAY_LABELS[index] ?? ""} · ${formatBranchDateLabel(dayStartUtc, branchTimeZoneId)}`,
        dayStartUtc
      };
    });
  }, [branchTimeZoneId, selectedDateUtc, view]);

  const totalHeight = hours.length * rowHeightPx;
  const hasAnyEvent = events.length > 0;

  return (
    <div
      className={cn(
        "overflow-hidden rounded-[var(--rs-radius-lg)] border border-[var(--rs-border)] bg-[var(--rs-glass)] shadow-[var(--rs-shadow-soft)] backdrop-blur-xl",
        className
      )}
    >
      <div
        className="grid"
        style={{
          gridTemplateColumns: `4.5rem repeat(${columns.length}, minmax(0, 1fr))`
        }}
      >
        <div className="border-b border-[var(--rs-border)] bg-[var(--rs-surface-muted)] px-3 py-2 text-xs font-medium uppercase tracking-[0.16em] text-[var(--rs-muted)]">
          Saat
        </div>
        {columns.map((column) => (
          <div
            key={column.key}
            className="border-b border-l border-[var(--rs-border)] px-3 py-2 text-xs font-medium text-[var(--rs-muted-strong)]"
          >
            {column.label}
          </div>
        ))}

        <div
          className="relative border-r border-[var(--rs-border)]"
          style={{ height: `${totalHeight}px` }}
        >
          {hours.map((hour) => (
            <div
              key={hour}
              className="flex items-start justify-end px-2 text-[0.65rem] tabular-nums text-[var(--rs-muted)]"
              style={{ height: `${rowHeightPx}px` }}
            >
              <span className="mt-1">{String(hour).padStart(2, "0")}:00</span>
            </div>
          ))}
        </div>

        {columns.map((column) => {
          const dayEvents = buildDayColumn({
            branchTimeZoneId,
            dayStartUtc: column.dayStartUtc,
            events
          });

          return (
            <div
              key={column.key}
              className="relative border-l border-[var(--rs-border)]"
              style={{ height: `${totalHeight}px` }}
            >
              {hours.map((hour) => (
                <div
                  key={hour}
                  className="border-b border-dashed border-[var(--rs-border)]"
                  style={{ height: `${rowHeightPx}px` }}
                />
              ))}

              {dayEvents.map((event) => {
                const layout = computeEventLayout({
                  branchTimeZoneId,
                  dayStartUtc: column.dayStartUtc,
                  event,
                  rowHeightPx,
                  workingHourEnd,
                  workingHourStart
                });
                if (!layout) {
                  return null;
                }

                const tone = event.tone ?? "accent";

                return (
                  <button
                    key={event.id}
                    type="button"
                    onClick={onEventClick ? () => onEventClick(event) : undefined}
                    style={{
                      top: `${layout.top}px`,
                      height: `${layout.height}px`
                    }}
                    className={cn(
                      "absolute inset-x-1 overflow-hidden rounded-xl border px-2.5 py-1.5 text-left transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--rs-focus)]",
                      toneStyles[tone],
                      onEventClick ? "hover:-translate-y-0.5 hover:shadow-[var(--rs-shadow-soft)]" : "cursor-default"
                    )}
                  >
                    <p className="truncate text-[0.7rem] font-semibold leading-tight">
                      {event.title}
                    </p>
                    {event.subtitle ? (
                      <p className="truncate text-[0.65rem] opacity-80">
                        {event.subtitle}
                      </p>
                    ) : null}
                    <p className="mt-0.5 text-[0.6rem] tabular-nums opacity-70">
                      {formatBranchTimeLabel(event.startUtc, branchTimeZoneId)} –{" "}
                      {formatBranchTimeLabel(event.endUtc, branchTimeZoneId)}
                    </p>
                    {event.meta ? <div className="mt-0.5">{event.meta}</div> : null}
                  </button>
                );
              })}

              {dayEvents.length === 0 && view === "day" ? (
                <p className="absolute inset-x-2 top-2 rounded-lg bg-[var(--rs-surface-muted)] px-2 py-1 text-[0.65rem] text-[var(--rs-muted)]">
                  {emptyHint}
                </p>
              ) : null}
            </div>
          );
        })}
      </div>

      {!hasAnyEvent ? (
        <p className="px-4 py-3 text-xs text-[var(--rs-muted)]">{emptyHint}</p>
      ) : null}
    </div>
  );
}
