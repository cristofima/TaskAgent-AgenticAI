import { describe, it, expect } from 'vitest';
import { PAGINATION, API } from '@/lib/constants';

describe('constants', () => {
  describe('PAGINATION', () => {
    it('should have correct default page size', () => {
      expect(PAGINATION.DEFAULT_PAGE_SIZE).toBe(20);
    });

    it('should have positive default page size', () => {
      expect(PAGINATION.DEFAULT_PAGE_SIZE).toBeGreaterThan(0);
    });

    it('should have max page size greater than default', () => {
      expect(PAGINATION.MAX_PAGE_SIZE).toBeGreaterThan(PAGINATION.DEFAULT_PAGE_SIZE);
    });

    it('should have conversation page size defined', () => {
      expect(PAGINATION.CONVERSATION_PAGE_SIZE).toBeDefined();
      expect(PAGINATION.CONVERSATION_PAGE_SIZE).toBeGreaterThan(0);
    });
  });

  describe('API', () => {
    it('should have BASE_URL defined', () => {
      expect(API.BASE_URL).toBeDefined();
      expect(typeof API.BASE_URL).toBe('string');
    });

    it('should have a valid URL format', () => {
      expect(API.BASE_URL).toMatch(/^https?:\/\//);
    });
  });
});
