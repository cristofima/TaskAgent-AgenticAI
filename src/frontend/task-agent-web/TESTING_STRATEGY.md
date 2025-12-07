# Testing Strategy Analysis - TaskAgent Frontend

## Project Overview

| Aspect | Details |
|--------|---------|
| **Framework** | Next.js 16.0.1 (App Router) |
| **React** | 19.2.0 |
| **TypeScript** | ✅ Configured with strict mode |
| **Components** | Chat (9), Conversations (4), Shared (2) |
| **Hooks** | 3 custom hooks |
| **Utilities** | date-utils.ts, constants.ts |

---

## Testing Framework Comparison

| Feature | **Vitest** | **Playwright** | **Jest** | **Cypress** |
|---------|------------|----------------|----------|-------------|
| **Type** | Unit/Component | E2E | Unit/Component | E2E + Component |
| **Speed** | ⚡ Very fast | 🔄 Moderate | 🔄 Moderate | 🐢 Slow |
| **Next.js Support** | ✅ Official | ✅ Official | ✅ Official | ✅ Official |
| **React 19** | ✅ | ✅ | ⚠️ Partial | ⚠️ Partial |
| **App Router** | ✅ | ✅ | ✅ | ✅ |
| **Server Components** | ⚠️ Sync only | ✅ E2E | ⚠️ Sync only | ✅ E2E |
| **Configuration** | 🟢 Simple | 🟢 Simple | 🟡 Medium | 🟡 Medium |
| **Watch Mode** | ✅ Native HMR | ⚠️ Limited | ✅ | ⚠️ Limited |
| **Vite Bundling** | ✅ Native | N/A | ❌ Webpack | ❌ Webpack |
| **Multi-browser** | ❌ jsdom | ✅ Chromium/FF/WebKit | ❌ jsdom | ✅ Chrome/FF/Edge |
| **CI/CD** | ✅ Easy | ✅ Easy | ✅ Easy | 🟡 Heavier |

---

## Recommendation: **Vitest + Playwright**

### Why This Combination?

#### Vitest for Unit Tests

- ⚡ ~10-20x faster than Jest (uses Vite internally)
- Compatible with TypeScript without extra configuration
- Jest-compatible API (easy migration if needed)
- Watch mode with instant HMR
- Native ESM support (Next.js App Router uses this)

#### Playwright for E2E

- Officially recommended by Next.js for async Server Components
- Multi-browser (Chromium, Firefox, WebKit) with a single API
- Intelligent auto-wait (less flakiness)
- Trace viewer for visual debugging
- Headless support for CI/CD

---

## Components to Test (Priority)

### Unit Tests (Vitest + React Testing Library)

| File | Type | Priority | Complexity |
|------|------|----------|------------|
| `lib/utils/date-utils.ts` | Utility | 🔴 High | 🟢 Low |
| `lib/constants.ts` | Constants | 🟡 Medium | 🟢 Low |
| `types/chat.ts` | Types | 🟡 Medium | 🟢 Low |
| `hooks/use-chat.ts` | Hook | 🔴 High | 🟠 Medium |
| `components/chat/ChatMessage.tsx` | Component | 🔴 High | 🟠 Medium |
| `components/chat/ChatInput.tsx` | Component | 🔴 High | 🟢 Low |

### E2E Tests (Playwright)

| Flow | Priority | Complexity |
|------|----------|------------|
| Initial navigation | 🔴 High | 🟢 Low |
| Send chat message | 🔴 High | 🟠 Medium |
| Create new conversation | 🔴 High | 🟠 Medium |
| Switch conversations | 🟡 Medium | 🟠 Medium |
| Delete conversation | 🟡 Medium | 🟠 Medium |
| Theme toggle (dark/light) | 🟡 Medium | 🟢 Low |

---

## ✅ Implemented Tests

> **Last Updated**: December 2025  
> **Total Tests**: 94 (57 unit + 37 E2E)

### Unit Tests (Vitest) - 57 Tests

| File | Tests | Status | Description |
|------|-------|--------|-------------|
| `__tests__/lib/constants.test.ts` | 6 | ✅ | PAGINATION and API constants validation |
| `__tests__/lib/utils/date-utils.test.ts` | 10 | ✅ | formatDistanceToNow, formatDate, formatDateTime |
| `__tests__/components/chat/ChatInput.test.tsx` | 19 | ✅ | Rendering, input handling, submit, loading, keyboard shortcuts, accessibility |
| `__tests__/components/chat/ChatMessage.test.tsx` | 22 | ✅ | User/assistant messages, suggestions, loading/streaming, function calls |

#### Unit Test Details

<details>
<summary><b>constants.test.ts</b> (6 tests)</summary>

- PAGINATION
  - ✅ should have correct default page size
  - ✅ should have positive default page size
  - ✅ should have max page size greater than default
  - ✅ should have conversation page size defined
- API
  - ✅ should have BASE_URL defined
  - ✅ should have ENDPOINTS defined

</details>

<details>
<summary><b>date-utils.test.ts</b> (10 tests)</summary>

- formatDistanceToNow
  - ✅ should return "just now" for dates less than a minute ago
  - ✅ should return minutes ago for dates less than an hour ago
  - ✅ should return hours ago for dates less than a day ago
  - ✅ should return days ago for dates less than a week ago
  - ✅ should return weeks ago for dates less than a month ago
  - ✅ should return months ago for dates less than a year ago
  - ✅ should return years ago for dates more than a year ago
- formatDate
  - ✅ should format date as "Mon DD, YYYY"
  - ✅ should handle different months correctly
- formatDateTime
  - ✅ should format date with time

</details>

<details>
<summary><b>ChatInput.test.tsx</b> (19 tests)</summary>

- rendering
  - ✅ should render textarea with placeholder
  - ✅ should render custom placeholder when provided
  - ✅ should render send button
  - ✅ should render helper text for keyboard shortcuts
- input handling
  - ✅ should display input value
  - ✅ should call handleInputChange when typing
- submit behavior
  - ✅ should call handleSubmit on form submit
  - ✅ should disable send button when input is empty
  - ✅ should disable send button when input is only whitespace
  - ✅ should enable send button when input has content
- loading state
  - ✅ should disable textarea when loading
  - ✅ should disable send button when loading
  - ✅ should show loading spinner when loading
- keyboard shortcuts
  - ✅ should submit form on Enter key press (without Shift)
  - ✅ should not submit form on Shift+Enter
  - ✅ should not submit on Enter when input is empty
  - ✅ should not submit on Enter when loading
- accessibility
  - ✅ should have proper aria-label on send button
  - ✅ should be focusable via ref

</details>

<details>
<summary><b>ChatMessage.test.tsx</b> (22 tests)</summary>

- User Messages
  - ✅ should render user message with correct styling
  - ✅ should display user message content
  - ✅ should not show suggestions for user messages
- Assistant Messages
  - ✅ should render assistant message with correct styling
  - ✅ should display assistant message content
  - ✅ should render markdown content in assistant messages
  - ✅ should render code blocks with syntax highlighting
- Suggestions
  - ✅ should render suggestions when provided
  - ✅ should call onSuggestionClick when suggestion is clicked
  - ✅ should not render suggestions section when array is empty
  - ✅ should not render suggestions when undefined
- Loading State
  - ✅ should show loading indicator when isLoading is true
  - ✅ should not show content when loading
  - ✅ should show loading for assistant messages only
- Streaming State
  - ✅ should show streaming indicator when isStreaming is true
  - ✅ should show content while streaming
  - ✅ should show streaming cursor animation
- Function Calls Filtering
  - ✅ should filter out function call JSON from content
  - ✅ should display content after function call JSON
  - ✅ should handle multiple function calls in content
- Empty Content
  - ✅ should handle empty content gracefully
  - ✅ should handle whitespace-only content

</details>

### E2E Tests (Playwright) - 37 Tests

| File | Tests | Status | Description |
|------|-------|--------|-------------|
| `e2e/navigation.spec.ts` | 6 | ✅ | Page loading, title, responsiveness, accessibility |
| `e2e/chat.spec.ts` | 7 | ✅ | Empty state, input, sending, loading, responses, errors |
| `e2e/conversations.spec.ts` | 10 | ✅ | Sidebar, list, create, switch, delete, persistence |
| `e2e/theme.spec.ts` | 14 | ✅ | Toggle, system preference, visual consistency, accessibility |

#### E2E Test Details

<details>
<summary><b>navigation.spec.ts</b> (6 tests)</summary>

- Navigation
  - ✅ should load the home page
  - ✅ should display the page title
  - ✅ should have a chat input area
  - ✅ should be responsive - mobile viewport
  - ✅ should be responsive - tablet viewport
- Accessibility
  - ✅ should have no accessibility violations on main elements

</details>

<details>
<summary><b>chat.spec.ts</b> (7 tests)</summary>

- Chat Interface
  - ✅ should display empty chat state initially
  - ✅ should allow typing in the chat input
  - ✅ should clear input after sending message
  - ✅ should show loading state while waiting for response
  - ✅ should display assistant messages with proper formatting
- Chat Error Handling
  - ✅ should handle API errors gracefully
  - ✅ should handle network timeout

</details>

<details>
<summary><b>conversations.spec.ts</b> (10 tests)</summary>

- Conversation Sidebar
  - ✅ should display sidebar toggle button
  - ✅ should have new chat button
- Conversation List
  - ✅ should load conversations list
- Create New Conversation
  - ✅ should create new conversation when sending first message
  - ✅ should clear messages when starting new conversation
- Switch Between Conversations
  - ✅ should maintain UI stability when switching
- Delete Conversation
  - ✅ should show delete confirmation modal
  - ✅ should close modal when cancelled
- Conversation Persistence
  - ✅ should maintain current thread ID in localStorage
  - ✅ should restore conversation on page reload

</details>

<details>
<summary><b>theme.spec.ts</b> (14 tests)</summary>

- Theme Toggle
  - ✅ should have theme toggle button
  - ✅ should toggle from light to dark mode
  - ✅ should toggle from dark to light mode
  - ✅ should persist theme preference in localStorage
  - ✅ should respect stored theme preference on reload
- System Color Scheme
  - ✅ should follow system dark mode preference
  - ✅ should follow system light mode preference
- Dark Mode Visual Consistency
  - ✅ should have readable text in dark mode
  - ✅ should have proper contrast in dark mode
  - ✅ should apply dark styles to chat messages
- Light Mode Visual Consistency
  - ✅ should have readable text in light mode
  - ✅ should have proper contrast in light mode
- Theme Accessibility
  - ✅ should maintain focus visibility in dark mode
  - ✅ should maintain focus visibility in light mode

</details>

### Test Coverage Summary

| Category | Planned | Implemented | Coverage |
|----------|---------|-------------|----------|
| **Unit Tests** | | | |
| Utilities (date-utils) | 🔴 High | ✅ 10 tests | 100% |
| Constants | 🟡 Medium | ✅ 6 tests | 100% |
| ChatInput Component | 🔴 High | ✅ 19 tests | 100% |
| ChatMessage Component | 🔴 High | ✅ 22 tests | 100% |
| use-chat Hook | 🔴 High | ⏳ Pending | 0% |
| **E2E Tests** | | | |
| Navigation | 🔴 High | ✅ 6 tests | 100% |
| Chat Flow | 🔴 High | ✅ 7 tests | 100% |
| Conversations | 🟡 Medium | ✅ 10 tests | 100% |
| Theme Toggle | 🟡 Medium | ✅ 14 tests | 100% |

### Pending Tests

| Test | Priority | Notes |
|------|----------|-------|
| `hooks/use-chat.ts` | 🔴 High | Complex hook with SSE streaming - consider E2E coverage |
| `hooks/use-conversations.ts` | 🟡 Medium | API integration hook |
| `types/chat.ts` | 🟢 Low | TypeScript validation at compile time |

---

## Required Dependencies

```json
{
  "devDependencies": {
    // Vitest + Testing Library (Unit Tests)
    "vitest": "^3.2.4",
    "@vitejs/plugin-react": "^4.4.0",
    "@testing-library/react": "^16.0.0",
    "@testing-library/dom": "^10.0.0",
    "jsdom": "^25.0.0",
    "vite-tsconfig-paths": "^5.1.0",
    
    // Playwright (E2E Tests)
    "@playwright/test": "^1.51.0"
  }
}
```

---

## Proposed Folder Structure

```
task-agent-web/
├── __tests__/                    # Unit tests (Vitest)
│   ├── lib/
│   │   └── utils/
│   │       └── date-utils.test.ts
│   ├── hooks/
│   │   └── use-chat.test.ts
│   └── components/
│       └── chat/
│           ├── ChatMessage.test.tsx
│           └── ChatInput.test.tsx
├── e2e/                          # E2E tests (Playwright)
│   ├── chat.spec.ts
│   ├── conversations.spec.ts
│   └── navigation.spec.ts
├── vitest.config.mts             # Vitest configuration
├── playwright.config.ts          # Playwright configuration
└── vitest-setup.ts               # Test setup file
```

---

## Important Considerations

### 1. API Mocking Strategy for E2E Tests

**Best Practice: Mock APIs for E2E tests** - This is the recommended approach by Playwright documentation.

#### Why Mock Instead of Real API?

| Aspect | Mocked API ✅ | Real API ❌ |
|--------|---------------|-------------|
| **Speed** | Instant responses | Network latency |
| **Reliability** | 100% predictable | Can fail for external reasons |
| **CI/CD** | No backend required | Requires infrastructure |
| **Isolation** | Tests only frontend | Tests frontend + backend |
| **Test Data** | Full control | Depends on backend state |
| **Cost** | Free | May have server costs |

#### Playwright Mocking Options

1. **Complete Mock** - Never calls the real API:
```typescript
await page.route('**/api/chat', async route => {
  await route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ message: 'Mocked response' }),
  });
});
```

2. **Modify Response** - Calls API but modifies the response:
```typescript
await page.route('**/api/data', async route => {
  const response = await route.fetch();
  const json = await response.json();
  json.extra = 'modified';
  await route.fulfill({ response, json });
});
```

3. **HAR Files** - Record real responses and replay them:
```typescript
await page.routeFromHAR('./fixtures/api.har', {
  url: '**/api/**',
  update: false, // Set true to record, false to replay
});
```

#### SSE Streaming Mock (for this project)

Since this project uses SSE for chat streaming:
```typescript
await page.route('**/api/agent/chat', async route => {
  const sseResponse = [
    'event: TEXT_MESSAGE_START\n',
    `data: {"messageId":"mock-1"}\n\n`,
    'event: TEXT_MESSAGE_CONTENT\n',
    'data: {"text":"Mocked response"}\n\n`,
    'event: TEXT_MESSAGE_END\n',
    'data: {}\n\n',
  ].join('');

  await route.fulfill({
    status: 200,
    contentType: 'text/event-stream',
    body: sseResponse,
  });
});
```

### 2. Async Server Components

Next.js documentation indicates that Vitest/Jest don't fully support async Server Components. For these, use **E2E tests with Playwright**.

### 3. React 19 Compatibility

Ensure using `@testing-library/react` v16+ which has full support for React 19.

### 4. SSE Streaming

The project uses SSE for chat streaming - this is difficult to test in unit tests. Better tested with E2E using mocked SSE responses.

### 5. Backend Dependency

E2E tests will require the .NET backend running. You can use mocks or a dedicated test environment.

---

## Proposed Scripts for `package.json`

```json
{
  "scripts": {
    "test": "vitest",
    "test:run": "vitest run",
    "test:coverage": "vitest run --coverage",
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui"
  }
}
```

---

## Implementation Phases

### Phase 1: Setup & Validation

1. Install dependencies
2. Configure Vitest (vitest.config.mts)
3. Configure Playwright (playwright.config.ts)
4. Create basic unit test (date-utils.test.ts) to validate setup

### Phase 2: Core Unit Tests

1. Utility functions tests
2. Custom hooks tests
3. Component rendering tests

### Phase 3: E2E Tests

1. Basic navigation tests
2. Chat flow tests
3. Conversation management tests

### Phase 4: CI/CD Integration

1. GitHub Actions workflow
2. Coverage reports
3. Test result artifacts

---

## References

- [Next.js Testing Guide](https://nextjs.org/docs/app/guides/testing)
- [Vitest Documentation](https://vitest.dev/)
- [Playwright Documentation](https://playwright.dev/)
- [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)
