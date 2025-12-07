#!/usr/bin/env python3
"""
Generate Architecture Diagrams for TaskAgent - AI-Powered Task Management.

Requirements:
    pip install diagrams

    Also requires Graphviz installed:
    - Windows: choco install graphviz OR https://graphviz.org/download/
    - Mac: brew install graphviz
    - Linux: apt-get install graphviz

Usage:
    python scripts/generate_architecture_diagram.py

Output:
    Creates PNG files in the docs/architecture/ directory:
    - architecture-main.png          (Main system overview)
    - architecture-clean.png         (Clean Architecture layers)
    - architecture-sse-flow.png      (SSE event streaming)
    - architecture-dual-database.png (SQL Server + PostgreSQL)
    - architecture-observability.png (OpenTelemetry + Aspire)
    - architecture-content-safety.png (Azure OpenAI filtering)
"""

from diagrams import Diagram, Cluster, Edge
from diagrams.azure.database import SQLDatabases, DatabaseForPostgresqlServers
from diagrams.azure.ml import AzureOpenAI, CognitiveServices
from diagrams.azure.devops import ApplicationInsights
from diagrams.azure.monitor import Monitor
from diagrams.onprem.client import User
from diagrams.programming.framework import React, NextJs, DotNet
from diagrams.programming.language import Csharp, Typescript
from diagrams.generic.compute import Rack
from diagrams.generic.storage import Storage
import os

# Get the directory where this script is located
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "..", "docs", "architecture")

# Create output directory if it doesn't exist
os.makedirs(OUTPUT_DIR, exist_ok=True)

# Common graph attributes for better readability
GRAPH_ATTR = {
    "fontsize": "18",
    "fontname": "Arial Bold",
    "bgcolor": "white",
    "pad": "0.4",
    "splines": "ortho",
    "nodesep": "0.8",
    "ranksep": "1.0",
}

# Node attributes - readable fonts
NODE_ATTR = {
    "fontsize": "12",
    "fontname": "Arial",
    "fontcolor": "#333333",
}

# Edge attributes
EDGE_ATTR = {
    "fontsize": "11",
    "fontname": "Arial",
    "fontcolor": "#555555",
    "penwidth": "1.5",
}

# Cluster attributes - Different colors for different layers
CLUSTER_FRONTEND = {
    "fontsize": "13",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#e3f2fd",  # Light blue
    "penwidth": "2",
    "margin": "16",
}

CLUSTER_BACKEND = {
    "fontsize": "13",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#e8f5e9",  # Light green
    "penwidth": "2",
    "margin": "16",
}

CLUSTER_DATABASE = {
    "fontsize": "13",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#fff3e0",  # Light orange
    "penwidth": "2",
    "margin": "16",
}

CLUSTER_AZURE = {
    "fontsize": "13",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#e1f5fe",  # Light cyan (Azure)
    "penwidth": "2",
    "margin": "16",
}

CLUSTER_OBSERVABILITY = {
    "fontsize": "13",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#f3e5f5",  # Light purple
    "penwidth": "2",
    "margin": "16",
}

# Clean Architecture layer colors
LAYER_PRESENTATION = {
    "fontsize": "12",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#bbdefb",  # Blue
    "penwidth": "2",
    "margin": "12",
}

LAYER_INFRASTRUCTURE = {
    "fontsize": "12",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#c8e6c9",  # Green
    "penwidth": "2",
    "margin": "12",
}

LAYER_APPLICATION = {
    "fontsize": "12",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#ffe0b2",  # Orange
    "penwidth": "2",
    "margin": "12",
}

LAYER_DOMAIN = {
    "fontsize": "12",
    "fontname": "Arial Bold",
    "fontcolor": "#1a1a1a",
    "style": "rounded",
    "bgcolor": "#ffcdd2",  # Red (core)
    "penwidth": "2",
    "margin": "12",
}


def create_main_architecture():
    """Create the main architecture diagram - System Overview."""
    
    with Diagram(
        "TaskAgent - AI-Powered Task Management",
        show=False,
        filename=os.path.join(OUTPUT_DIR, "architecture-main"),
        outformat="png",
        graph_attr=GRAPH_ATTR,
        node_attr=NODE_ATTR,
        edge_attr=EDGE_ATTR,
        direction="TB"
    ):
        user = User("User")
        
        # Frontend Layer
        with Cluster("Frontend (Next.js 16)", graph_attr=CLUSTER_FRONTEND):
            nextjs = NextJs("App Router")
            chat_ui = Typescript("Chat UI")
            sse_client = Typescript("SSE Client")
        
        # Backend Layer
        with Cluster("Backend (.NET 10)", graph_attr=CLUSTER_BACKEND):
            webapi = DotNet("Web API")
            agent = DotNet("Agent Framework")
            functions = Csharp("Function Tools")
        
        # Database Layer
        with Cluster("Databases", graph_attr=CLUSTER_DATABASE):
            sqlserver = SQLDatabases("SQL Server")
            postgres = DatabaseForPostgresqlServers("PostgreSQL")
        
        # Azure Services
        with Cluster("Azure Services", graph_attr=CLUSTER_AZURE):
            openai = AzureOpenAI("GPT-4o-mini")
            appinsights = ApplicationInsights("App Insights")
        
        # Flow connections
        user >> nextjs
        nextjs >> chat_ui >> sse_client
        sse_client >> Edge(label="SSE Stream", fontsize="11") >> webapi
        webapi >> agent >> functions
        functions >> sqlserver
        agent >> postgres
        agent >> openai
        webapi >> appinsights


def create_clean_architecture():
    """Create Clean Architecture layers diagram."""
    
    with Diagram(
        "TaskAgent - Clean Architecture",
        show=False,
        filename=os.path.join(OUTPUT_DIR, "architecture-clean"),
        outformat="png",
        graph_attr={**GRAPH_ATTR, "ranksep": "0.8"},
        node_attr=NODE_ATTR,
        edge_attr=EDGE_ATTR,
        direction="TB"
    ):
        # Presentation Layer (WebApi uses DotNet)
        with Cluster("Presentation (WebApi)", graph_attr=LAYER_PRESENTATION):
            controllers = DotNet("Controllers")
            streaming = DotNet("SSE Service")
            middleware = DotNet("Middleware")
        
        # Infrastructure Layer
        with Cluster("Infrastructure", graph_attr=LAYER_INFRASTRUCTURE):
            dbcontext = Csharp("DbContexts")
            repositories = Csharp("Repositories")
            services = Csharp("Services")
        
        # Application Layer
        with Cluster("Application", graph_attr=LAYER_APPLICATION):
            dtos = Csharp("DTOs")
            interfaces = Csharp("Interfaces")
            functions = Csharp("Functions (6)")
            telemetry = Csharp("Telemetry")
        
        # Domain Layer
        with Cluster("Domain", graph_attr=LAYER_DOMAIN):
            entities = Csharp("Entities")
            enums = Csharp("Enums")
            rules = Csharp("Business Rules")
        
        # Dependency flow (downward only - Clean Architecture)
        controllers >> Edge(style="dashed", color="#1976d2") >> services
        streaming >> Edge(style="dashed", color="#1976d2") >> services
        
        dbcontext >> Edge(style="dashed", color="#388e3c") >> interfaces
        repositories >> Edge(style="dashed", color="#388e3c") >> interfaces
        services >> Edge(style="dashed", color="#388e3c") >> functions
        
        dtos >> Edge(style="dashed", color="#f57c00") >> entities
        interfaces >> Edge(style="dashed", color="#f57c00") >> entities
        functions >> Edge(style="dashed", color="#f57c00") >> entities


def create_sse_flow_diagram():
    """Create SSE Event Streaming flow diagram."""
    
    sse_graph = {**GRAPH_ATTR, "nodesep": "0.5", "ranksep": "0.6", "splines": "polyline"}
    
    with Diagram(
        "TaskAgent - SSE Event Flow",
        show=False,
        filename=os.path.join(OUTPUT_DIR, "architecture-sse-flow"),
        outformat="png",
        graph_attr=sse_graph,
        node_attr=NODE_ATTR,
        edge_attr=EDGE_ATTR,
        direction="LR"
    ):
        # Frontend
        with Cluster("Frontend", graph_attr=CLUSTER_FRONTEND):
            chat_input = NextJs("ChatInput")
            use_chat = Typescript("useChat")
            messages = NextJs("Messages")
        
        # Backend
        with Cluster("Backend", graph_attr=CLUSTER_BACKEND):
            controller = DotNet("Controller")
            agent_service = DotNet("Streaming")
            agent = Csharp("Agent")
        
        # SSE Connection
        with Cluster("SSE", graph_attr=CLUSTER_AZURE):
            sse_events = Rack("Events")
        
        # Flow
        chat_input >> use_chat
        use_chat >> controller
        controller >> agent_service >> agent
        agent >> Edge(style="dashed") >> sse_events
        sse_events >> Edge(color="#1976d2") >> messages


def create_dual_database_diagram():
    """Create Dual Database Architecture diagram."""
    
    with Diagram(
        "TaskAgent - Dual Database",
        show=False,
        filename=os.path.join(OUTPUT_DIR, "architecture-dual-database"),
        outformat="png",
        graph_attr={**GRAPH_ATTR, "ranksep": "0.7"},
        node_attr=NODE_ATTR,
        edge_attr=EDGE_ATTR,
        direction="TB"
    ):
        # Application
        agent = DotNet("Agent Framework")
        
        # SQL Server cluster
        with Cluster("SQL Server (Tasks)", graph_attr={**CLUSTER_DATABASE, "bgcolor": "#e3f2fd"}):
            task_repo = Csharp("Repository")
            task_context = Csharp("DbContext")
            task_table = SQLDatabases("Tasks")
        
        # PostgreSQL cluster
        with Cluster("PostgreSQL (Chats)", graph_attr={**CLUSTER_DATABASE, "bgcolor": "#e8f5e9"}):
            thread_service = Csharp("Persistence")
            conv_context = Csharp("DbContext")
            conv_table = DatabaseForPostgresqlServers("Threads")
        
        # Connections
        agent >> Edge(label="Tasks") >> task_repo
        task_repo >> task_context >> task_table
        
        agent >> Edge(label="Chats") >> thread_service
        thread_service >> conv_context >> conv_table


def create_observability_diagram():
    """Create Observability Stack diagram."""
    
    with Diagram(
        "TaskAgent - Observability",
        show=False,
        filename=os.path.join(OUTPUT_DIR, "architecture-observability"),
        outformat="png",
        graph_attr={**GRAPH_ATTR, "ranksep": "0.7"},
        node_attr=NODE_ATTR,
        edge_attr=EDGE_ATTR,
        direction="TB"
    ):
        # Application
        webapi = DotNet("WebApi")
        
        # Telemetry sources
        with Cluster("Telemetry", graph_attr=CLUSTER_BACKEND):
            serilog = Csharp("Serilog")
            otel_traces = Csharp("Tracing")
            otel_metrics = Csharp("Metrics")
        
        # Development
        with Cluster("Development", graph_attr=CLUSTER_AZURE):
            aspire = ApplicationInsights("Aspire")
        
        # Production
        with Cluster("Production", graph_attr=CLUSTER_OBSERVABILITY):
            app_insights = ApplicationInsights("App Insights")
            azure_monitor = Monitor("Monitor")
        
        # Connections
        webapi >> serilog
        webapi >> otel_traces
        webapi >> otel_metrics
        
        serilog >> Edge(label="OTLP") >> aspire
        otel_traces >> aspire
        otel_metrics >> aspire
        
        serilog >> Edge(style="dashed") >> app_insights
        otel_traces >> Edge(style="dashed") >> app_insights
        otel_metrics >> Edge(style="dashed") >> azure_monitor


def create_content_safety_diagram():
    """Create Content Safety Flow diagram."""
    
    safety_graph = {**GRAPH_ATTR, "nodesep": "0.6", "ranksep": "0.6", "splines": "polyline"}
    
    with Diagram(
        "TaskAgent - Content Safety",
        show=False,
        filename=os.path.join(OUTPUT_DIR, "architecture-content-safety"),
        outformat="png",
        graph_attr=safety_graph,
        node_attr=NODE_ATTR,
        edge_attr=EDGE_ATTR,
        direction="LR"
    ):
        # User
        user = User("User")
        
        # Frontend
        with Cluster("Frontend", graph_attr=CLUSTER_FRONTEND):
            chat_input = NextJs("Input")
            chat_message = NextJs("Output")
        
        # Backend
        with Cluster("Backend", graph_attr=CLUSTER_BACKEND):
            agent_service = DotNet("Service")
        
        # Azure OpenAI
        with Cluster("Azure OpenAI", graph_attr=CLUSTER_AZURE):
            openai = AzureOpenAI("GPT-4o-mini")
            content_filter = CognitiveServices("Filter")
        
        # User flow
        user >> chat_input
        
        # Happy path (green)
        chat_input >> agent_service
        agent_service >> openai >> content_filter
        content_filter >> Edge(color="#4caf50") >> agent_service
        agent_service >> Edge(color="#4caf50") >> chat_message
        
        # Blocked path (red) - simplified
        content_filter >> Edge(color="#f44336", style="dashed") >> chat_message


if __name__ == "__main__":
    print("=" * 60)
    print("TaskAgent - Architecture Diagram Generator")
    print("=" * 60)
    print(f"\nOutput directory: {OUTPUT_DIR}")
    
    try:
        print("\n1. Creating main architecture diagram...")
        create_main_architecture()
        print("   ✅ architecture-main.png")
        
        print("\n2. Creating Clean Architecture layers diagram...")
        create_clean_architecture()
        print("   ✅ architecture-clean.png")
        
        print("\n3. Creating SSE event flow diagram...")
        create_sse_flow_diagram()
        print("   ✅ architecture-sse-flow.png")
        
        print("\n4. Creating dual database architecture diagram...")
        create_dual_database_diagram()
        print("   ✅ architecture-dual-database.png")
        
        print("\n5. Creating observability stack diagram...")
        create_observability_diagram()
        print("   ✅ architecture-observability.png")
        
        print("\n6. Creating content safety flow diagram...")
        create_content_safety_diagram()
        print("   ✅ architecture-content-safety.png")
        
        print(f"\n{'=' * 60}")
        print(f"✅ All diagrams generated in: {OUTPUT_DIR}")
        print("=" * 60)
        print("\nAvailable diagrams:")
        print("  📊 architecture-main.png           (System overview)")
        print("  🏛️  architecture-clean.png          (Clean Architecture layers)")
        print("  📡 architecture-sse-flow.png       (SSE event streaming)")
        print("  💾 architecture-dual-database.png  (SQL Server + PostgreSQL)")
        print("  📈 architecture-observability.png  (OpenTelemetry + Aspire)")
        print("  🛡️  architecture-content-safety.png (Azure OpenAI filtering)")
        
    except Exception as e:
        print(f"\n❌ Error: {e}")
        import traceback
        traceback.print_exc()
        print("\n" + "=" * 60)
        print("Make sure you have installed:")
        print("  pip install diagrams")
        print("  And Graphviz: https://graphviz.org/download/")
        print("=" * 60)
