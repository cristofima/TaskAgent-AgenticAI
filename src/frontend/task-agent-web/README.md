# TaskAgent Web - Frontend

AI-powered task management interface built with **Next.js 16**, **React 19**, and **TypeScript**.

## ✨ Features

### Core Functionality

- 🤖 **AI Chat Interface** - Natural language task management
- 🎯 **Custom AG-UI Integration** - SSE streaming with `/api/agent/chat` endpoint
- 💡 **Smart Suggestions** - Clickable contextual suggestions from AI agent
- 📂 **Chat Management** - List, load, and delete chats
- 🗂️ **Sidebar Navigation** - Collapsible sidebar with chat history
- 🏷️ **Auto-generated Titles** - Titles extracted from first user message
- ⏳ **Enhanced Loading States** - Contextual loading messages with animations
- 📱 **Responsive Design** - Works on desktop, tablet, and mobile
- 🎨 **Modern UI** - ChatGPT-inspired clean design with Tailwind CSS
- 📐 **Adaptive Layout** - Centered welcome state, fixed input when chatting
- 🔄 **Smart Scrolling** - Independent message scroll with fixed header and input
- 💾 **localStorage Persistence** - Remembers current chat across sessions

### Recent Updates (November 2025)

#### v2.1 - Content Safety UX Enhancements

- ✅ **Blocked Messages in Chat** - Content Safety violations appear as assistant messages (not toasts)
- ✅ **Thread Continuity** - Blocked chats create threads for seamless continuation
- ✅ **Smart Title Updates** - Titles regenerate when first valid message sent after block
- ✅ **Optimized Sidebar Refresh** - Only reloads when title changes (efficient flag-based approach)
- ✅ **ChatGPT-like Behavior** - Natural chat flow even with blocked messages

#### v2.0 - Chat Management

- ✅ **ConversationSidebar Component** - Full chat history with search
- ✅ **ConversationList Component** - Paginated list with auto-generated titles
- ✅ **ConversationItem Component** - Individual chat cards with metadata
- ✅ **DeleteConfirmModal Component** - Confirmation dialog with smooth animations
- ✅ **useConversations Hook** - Chat state management
- ✅ **localStorage Integration** - Persists current thread ID
- ✅ **API Integration** - List, load, and delete endpoints

#### v1.0 - Chat Interface

- ✅ **ChatGPT-Inspired Layout** - Full-height chat with adaptive behavior
- ✅ **SuggestionsBar Component** - Click suggestions to send messages
- ✅ **LoadingIndicator Component** - Rotating contextual messages
- ✅ **Improved UX** - Smooth animations and visual feedback
- ✅ **Minimalist Header** - Compact header only when messages exist
- ✅ **Optimized Input** - Icon-based send button with hover states
- ✅ **Type-safe** - Full TypeScript with strict mode

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ or 20+
- pnpm (recommended) or npm
- .NET backend running at `https://localhost:5001`

### Installation

```bash
# Install dependencies
pnpm install

# Run development server
pnpm dev

# Build for production
pnpm build

# Start production server
pnpm start
```

Open [http://localhost:3000](http://localhost:3000) to see the application.

### Environment Variables

**For Local Development:**

Create a `.env.local` file:

```bash
# Backend API URL (Next.js public env var)
NEXT_PUBLIC_API_URL=https://localhost:5001
```

**For Production (Azure Static Web Apps):**

Environment variables must be configured **at build time** as GitHub repository secrets because Next.js static export (`output: "export"`) doesn't support runtime environment variables.

1. **Add GitHub Secret**:

   - Go to **Settings** → **Secrets and variables** → **Actions**
   - Click **"New repository secret"**
   - Name: `NEXT_PUBLIC_API_URL`
   - Value: Your production backend URL (e.g., `https://app-taskagent-prod.azurewebsites.net`)

2. **Verify GitHub Actions Workflow**:
   The `frontend.yml` workflow should include:
   ```yaml
   - name: Build Next.js
     env:
       NEXT_PUBLIC_API_URL: ${{ secrets.NEXT_PUBLIC_API_URL }}
     run: |
       cd src/frontend/task-agent-web
       pnpm build
   ```

**Why this approach?**

- ✅ Next.js replaces `process.env.NEXT_PUBLIC_API_URL` with actual value during build
- ✅ Static files include the correct backend URL
- ❌ Azure Static Web Apps **cannot** inject runtime variables into pre-built static files
- ❌ Environment variables in Azure portal don't work with static exports

## 📂 Project Structure

```
src/frontend/task-agent-web/
├── app/                        # Next.js App Router
│   ├── layout.tsx              # Root layout
│   ├── page.tsx                # Home page (chat interface)
│   └── globals.css             # Global styles
├── components/                 # React components
│   ├── chat/                   # Chat-specific components
│   │   ├── ChatInterface.tsx           # Main chat component (adaptive layout)
│   │   ├── ChatInterfaceClient.tsx     # Client wrapper (dynamic loading)
│   │   ├── ChatMessagesList.tsx        # Messages container (conditional layout)
│   │   ├── ChatMessage.tsx             # Individual message bubble
│   │   ├── ChatInput.tsx               # Input field (icon-based send)
│   │   ├── ChatHeader.tsx              # Minimalist header
│   │   ├── EmptyChatState.tsx          # Welcome state
│   │   ├── SuggestionsBar.tsx          # Clickable suggestion buttons
│   │   ├── ErrorToast.tsx              # Error display
│   │   └── LoadingIndicator.tsx        # Contextual loading states
│   ├── conversations/          # Chat management
│   │   ├── ConversationSidebar.tsx     # Sidebar layout
│   │   ├── ConversationList.tsx        # List of chats
│   │   ├── ConversationItem.tsx        # Individual chat card
│   │   └── DeleteConfirmModal.tsx      # Delete confirmation
│   └── shared/                 # Shared components
│       └── LoadingIndicator.tsx    # Reusable loading component
├── hooks/                      # Custom React hooks
│   ├── use-chat.ts             # Chat state management
│   └── use-conversations.ts    # Chat management
├── lib/                        # Utilities
│   ├── utils.ts                # Helper functions (cn utility)
│   ├── constants.ts            # App constants
│   └── api/                    # API client functions
│       └── chat-service.ts     # Chat & API client
├── types/                     # TypeScript definitions
│   ├── chat.ts                # Chat types
│   └── conversation.ts        # Thread/conversation types (technical)
├── public/                    # Static assets
└── types/ # TypeScript definitions
    └── chat.ts # Chat types
```

## 🏗️ Architecture

**Custom Implementation with AG-UI Foundation**:

```
Frontend (Next.js)
├── Custom UI Components
│   ├── ChatInterface.tsx
│   ├── ConversationSidebar.tsx
│   ├── ChatMessagesList.tsx
│   └── use-chat.ts hook
│
↕️ SSE Streaming (Server-Sent Events)
│   POST /api/agent/chat
│   • serializedState → Backend
│   • SSE events ← Backend
│   • THREAD_STATE event (new serializedState)
│
Backend (.NET)
├── AgentController (Custom SSE endpoint)
│   └── Wraps Microsoft Agent Framework
│       • Deserializes thread from serializedState
│       • Streams responses via RunStreamingAsync
│       • Returns updated serializedState
│
└── PostgresChatMessageStore
    └── Automatic persistence in PostgreSQL
```

**Why Custom AG-UI Endpoint (not standard `/agui`)?**
- ✅ **Full SSE control**: Custom event types (`CONTENT_START`, `CONTENT_DELTA`, `THREAD_STATE`)
- ✅ **serializedState pattern**: Frontend receives updated state after each response
- ✅ **Chat continuity**: Backend deserializes full thread from PostgreSQL
- ✅ **No protocol limitations**: Can add custom events as needed
- ✅ **Integrated chat sidebar**: 291 lines with auto-generated titles
- ❌ Standard `/agui` doesn't return `serializedState` in streaming mode

**Why Custom UI (not CopilotKit)?**
- ✅ **Chat-first application**: Not auxiliary chat over another app
- ✅ **Full UX control**: ChatGPT-inspired adaptive layout
- ✅ **Minimal dependencies**: No heavy UI framework
- ❌ CopilotKit designed for auxiliary chat, not main application

**Microsoft Agent Framework Benefits**:
- 🔄 Automatic message persistence via `ChatMessageStore`
- 📡 SSE streaming with `RunStreamingAsync`
- 🧵 Thread serialization/deserialization built-in
- 📦 Function calling with `AIFunctionFactory`

## 🎯 Key Technologies

- **Next.js 16** - React framework with App Router
- **React 19** - UI library with Server Components
- **TypeScript** - Type safety
- **Tailwind CSS 4** - Utility-first CSS
- **pnpm** - Fast, efficient package manager
- **ESLint** - Code quality

## 🧪 Testing

```bash
# Run linter
pnpm lint

# Type check
pnpm build

# Run tests (when available)
pnpm test
```

## 🎨 Customization

### Styling

All styles use Tailwind CSS. Customize in:

- `tailwind.config.ts` - Theme configuration
- `app/globals.css` - Global styles
- Component files - Component-specific styles

### Colors

Main color palette:

- **Primary**: Blue (blue-500 to blue-700)
- **Background**: Gray (gray-50 to gray-900)
- **Suggestions**: Blue gradient (blue-50 to blue-200)

### Typography

- Font: Geist (optimized by Next.js)
- Sizes: Tailwind's default scale

## 📡 Backend Integration

### API Endpoints Used

#### Chat Endpoints

- `POST /api/Chat/send` - Send message (non-streaming)
- `POST /api/Chat/stream` - Streaming support (paused for future releases)

#### Chat Management

- `GET /api/Chat/threads` - List chats with pagination
- `GET /api/Chat/threads/{threadId}/messages` - Get chat history
- `DELETE /api/Chat/threads/{threadId}` - Delete chat

### Request/Response Formats

#### Send Message

```typescript
// Request
POST /api/Chat/send
{
  "message": "Create a high priority task",
  "threadId": "abc-123-def"  // Optional, creates new if null
}

// Response
{
  "message": "✅ Task created successfully",
  "threadId": "abc-123-def",
  "messageId": "msg-456",
  "createdAt": "2025-11-17T10:30:00Z",
  "suggestions": ["View all tasks", "Create another task"],
  "metadata": {
    "functionCalls": [
      {
        "functionName": "CreateTask",
        "arguments": { "title": "...", "priority": "High" },
        "result": "✅ Task created"
      }
    ]
  }
}
```

#### List Chats

```typescript
// Request
GET /api/Chat/threads?page=1&pageSize=20&sortBy=UpdatedAt&sortOrder=desc&isActive=true

// Response
{
  "threads": [
    {
      "id": "abc-123-def",
      "title": "Create a high priority task to review quarterl...",
      "preview": "✅ Task created successfully. I've added a high...",
      "messageCount": 5,
      "createdAt": "2025-11-17T10:00:00Z",
      "updatedAt": "2025-11-17T10:35:00Z",
      "isActive": true
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

#### Get Chat History

```typescript
// Request
GET /api/Chat/threads/abc-123-def/messages?page=1&pageSize=50

// Response
{
  "messages": [
    {
      "id": "msg-123",
      "role": "user",
      "content": "Create a high priority task",
      "timestamp": "2025-11-17T10:30:00Z"
    },
    {
      "id": "msg-124",
      "role": "assistant",
      "content": "✅ Task created successfully...",
      "timestamp": "2025-11-17T10:30:05Z"
    }
  ],
  "threadId": "abc-123-def",
  "totalCount": 5,
  "page": 1,
  "pageSize": 50
}
```

### Type Definitions

```typescript
// types/chat.ts
export interface ChatMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  timestamp: string;
}

export interface ChatResponse {
  message: string;
  threadId: string;
  messageId: string;
  createdAt: string;
  metadata?: MessageMetadata;
  suggestions?: string[];
}

// types/conversation.ts
export interface ConversationThread {
  id: string;
  title: string;
  preview: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
  isActive: boolean;
}

export interface ListThreadsResponse {
  threads: ConversationThread[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

## 🧪 Testing

### Manual Testing Guide

For comprehensive end-to-end testing scenarios including:

- Suggestions UI testing
- Loading states validation
- Content Safety blocked message flow
- Sidebar update behavior
- Error handling

**See**: [docs/FRONTEND_E2E_TESTING.md](../../../../../docs/FRONTEND_E2E_TESTING.md)

### Unit Testing (Planned)

```bash
# Run tests (when available)
pnpm test

# Run tests in watch mode
pnpm test:watch
```

### E2E Testing (Planned)

```bash
# Run Playwright E2E tests
pnpm test:e2e
```

---

## 🔧 Development

### Code Quality Standards

- ✅ TypeScript strict mode
- ✅ ESLint with Next.js rules
- ✅ Prettier formatting
- ✅ SOLID principles
- ✅ Clean Architecture

### Component Patterns

- **Server Components** by default
- **Client Components** (`"use client"`) only when needed
- **Custom hooks** for logic reuse
- **Composition** over inheritance

## 🚢 Deployment

### Vercel (Recommended)

```bash
# Deploy to Vercel
vercel

# Or connect GitHub repo for auto-deployments
```

### Docker

```dockerfile
# Dockerfile included in project
docker build -t task-agent-web .
docker run -p 3000:3000 task-agent-web
```

### Environment Variables for Production

```bash
NEXT_PUBLIC_API_URL=https://your-backend-api.com
PORT=3000  # Optional, defaults to 3000
```

## 🤝 Contributing

1. Follow TypeScript strict mode
2. Use functional components
3. Write self-documenting code
4. Add JSDoc comments for public APIs
5. Test manually before committing

## 📄 License

See [LICENSE](../../LICENSE) file in root directory.

## 🔗 Links

- **Backend**: [TaskAgent.WebApp](../../backend/services/TaskAgent/src/TaskAgent.WebApp/)
- **Next.js Docs**: https://nextjs.org/docs
- **React Docs**: https://react.dev
- **Tailwind CSS**: https://tailwindcss.com

---

**Built with ❤️ using modern web technologies**
