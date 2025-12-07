import { describe, it, expect } from 'vitest';
import {
  formatDistanceToNow,
  formatDate,
  formatDateTime,
} from '@/lib/utils/date-utils';

describe('date-utils', () => {
  describe('formatDistanceToNow', () => {
    it('should return "just now" for dates less than a minute ago', () => {
      const now = new Date();
      const thirtySecondsAgo = new Date(now.getTime() - 30 * 1000);

      expect(formatDistanceToNow(thirtySecondsAgo)).toBe('just now');
    });

    it('should return minutes ago for dates less than an hour ago', () => {
      const now = new Date();
      const fiveMinutesAgo = new Date(now.getTime() - 5 * 60 * 1000);

      expect(formatDistanceToNow(fiveMinutesAgo)).toBe('5m ago');
    });

    it('should return hours ago for dates less than a day ago', () => {
      const now = new Date();
      const threeHoursAgo = new Date(now.getTime() - 3 * 60 * 60 * 1000);

      expect(formatDistanceToNow(threeHoursAgo)).toBe('3h ago');
    });

    it('should return days ago for dates less than a week ago', () => {
      const now = new Date();
      const twoDaysAgo = new Date(now.getTime() - 2 * 24 * 60 * 60 * 1000);

      expect(formatDistanceToNow(twoDaysAgo)).toBe('2d ago');
    });

    it('should return weeks ago for dates less than a month ago', () => {
      const now = new Date();
      const twoWeeksAgo = new Date(now.getTime() - 14 * 24 * 60 * 60 * 1000);

      expect(formatDistanceToNow(twoWeeksAgo)).toBe('2w ago');
    });

    it('should return months ago for dates less than a year ago', () => {
      const now = new Date();
      const threeMonthsAgo = new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);

      expect(formatDistanceToNow(threeMonthsAgo)).toBe('3mo ago');
    });

    it('should return years ago for dates more than a year ago', () => {
      const now = new Date();
      const twoYearsAgo = new Date(now.getTime() - 730 * 24 * 60 * 60 * 1000);

      expect(formatDistanceToNow(twoYearsAgo)).toBe('2y ago');
    });
  });

  describe('formatDate', () => {
    it('should format date as "Mon DD, YYYY"', () => {
      const date = new Date('2025-12-06T10:30:00');

      expect(formatDate(date)).toBe('Dec 6, 2025');
    });

    it('should handle different months correctly', () => {
      const january = new Date('2025-01-15T10:30:00');
      const july = new Date('2025-07-04T10:30:00');

      expect(formatDate(january)).toBe('Jan 15, 2025');
      expect(formatDate(july)).toBe('Jul 4, 2025');
    });
  });

  describe('formatDateTime', () => {
    it('should format date with time', () => {
      const date = new Date('2025-12-06T14:30:00');

      // Note: Output depends on locale, but should contain the date and time
      const result = formatDateTime(date);

      expect(result).toContain('Dec');
      expect(result).toContain('6');
      expect(result).toContain('2025');
      // Time part (2:30 PM or 14:30 depending on locale)
      expect(result).toMatch(/2:30|14:30/);
    });
  });
});
