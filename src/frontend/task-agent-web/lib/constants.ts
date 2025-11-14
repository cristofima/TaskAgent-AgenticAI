/**
 * Application-wide constants
 * Centralized configuration values to avoid magic numbers
 */

/**
 * Pagination settings for various list views
 */
export const PAGINATION = {
    /** Default page size for general lists */
    DEFAULT_PAGE_SIZE: 20,
    /** Maximum allowed page size */
    MAX_PAGE_SIZE: 50,
    /** Page size for conversation/message history */
    CONVERSATION_PAGE_SIZE: 50,
} as const;

/**
 * API configuration
 */
export const API = {
    /** Base URL for backend API */
    BASE_URL: process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000",
} as const;
