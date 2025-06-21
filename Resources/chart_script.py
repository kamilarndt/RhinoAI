import plotly.graph_objects as go
import plotly.express as px
import json
import numpy as np

# Data from the provided JSON
data = {
  "components": [
    {"name": "User Input", "layer": "Input", "type": "input", "color": "#1FB8CD"},
    {"name": "Intent Classifier", "layer": "Intent", "type": "processing", "color": "#1FB8CD"},
    {"name": "Confidence Scoring", "layer": "Intent", "type": "processing", "color": "#1FB8CD"},
    {"name": "Context Manager", "layer": "Context", "type": "processing", "color": "#FFC185"},
    {"name": "Conversation History", "layer": "Context", "type": "storage", "color": "#FFC185"},
    {"name": "Scene Awareness", "layer": "Context", "type": "processing", "color": "#FFC185"},
    {"name": "OpenAI Client", "layer": "Processing", "type": "ai", "color": "#ECEBD5"},
    {"name": "Claude Client", "layer": "Processing", "type": "ai", "color": "#ECEBD5"},
    {"name": "Response Cache", "layer": "Processing", "type": "storage", "color": "#5D878F"},
    {"name": "Fallback Handler", "layer": "Processing", "type": "processing", "color": "#D2BA4C"},
    {"name": "Parameter Extractor", "layer": "Validation", "type": "processing", "color": "#B4413C"},
    {"name": "Semantic Validator", "layer": "Validation", "type": "processing", "color": "#B4413C"},
    {"name": "Command Executor", "layer": "Execution", "type": "processing", "color": "#964325"},
    {"name": "Retry Mechanism", "layer": "Execution", "type": "processing", "color": "#964325"},
    {"name": "Rhino API", "layer": "Execution", "type": "external", "color": "#944454"},
    {"name": "Response Formatter", "layer": "Response", "type": "processing", "color": "#13343B"},
    {"name": "Error Handler", "layer": "Response", "type": "processing", "color": "#DB4545"},
    {"name": "User Feedback", "layer": "Response", "type": "output", "color": "#13343B"}
  ],
  "connections": [
    {"from": "User Input", "to": "Intent Classifier", "type": "data"},
    {"from": "Intent Classifier", "to": "Confidence Scoring", "type": "data"},
    {"from": "User Input", "to": "Context Manager", "type": "data"},
    {"from": "Context Manager", "to": "Conversation History", "type": "storage"},
    {"from": "Context Manager", "to": "Scene Awareness", "type": "data"},
    {"from": "Confidence Scoring", "to": "OpenAI Client", "type": "ai"},
    {"from": "Confidence Scoring", "to": "Claude Client", "type": "ai"},
    {"from": "Confidence Scoring", "to": "Fallback Handler", "type": "error"},
    {"from": "OpenAI Client", "to": "Response Cache", "type": "cache"},
    {"from": "Claude Client", "to": "Response Cache", "type": "cache"},
    {"from": "OpenAI Client", "to": "Parameter Extractor", "type": "data"},
    {"from": "Claude Client", "to": "Parameter Extractor", "type": "data"},
    {"from": "Parameter Extractor", "to": "Semantic Validator", "type": "validation"},
    {"from": "Semantic Validator", "to": "Command Executor", "type": "validation"},
    {"from": "Semantic Validator", "to": "Error Handler", "type": "error"},
    {"from": "Command Executor", "to": "Retry Mechanism", "type": "error"},
    {"from": "Command Executor", "to": "Rhino API", "type": "data"},
    {"from": "Retry Mechanism", "to": "Command Executor", "type": "feedback"},
    {"from": "Command Executor", "to": "Response Formatter", "type": "data"},
    {"from": "Error Handler", "to": "Response Formatter", "type": "error"},
    {"from": "Fallback Handler", "to": "Response Formatter", "type": "error"},
    {"from": "Response Formatter", "to": "User Feedback", "type": "data"},
    {"from": "User Feedback", "to": "Context Manager", "type": "feedback"}
  ]
}

# Define layer order and positions with increased spacing
layer_order = ["Input", "Intent", "Context", "Processing", "Validation", "Execution", "Response"]
layer_y = {layer: i * 2 for i, layer in enumerate(layer_order)}  # Increased vertical spacing

# Create node positions with better spacing
node_positions = {}
layer_counts = {}

# Count components per layer
for comp in data["components"]:
    layer = comp["layer"]
    if layer not in layer_counts:
        layer_counts[layer] = 0
    layer_counts[layer] += 1

# Assign positions with wider horizontal spacing
layer_indices = {}
for comp in data["components"]:
    layer = comp["layer"]
    if layer not in layer_indices:
        layer_indices[layer] = 0
    
    # Calculate x position with wider spacing
    total_in_layer = layer_counts[layer]
    if total_in_layer == 1:
        x_pos = 0
    else:
        x_pos = (layer_indices[layer] - (total_in_layer - 1) / 2) * 3  # Increased horizontal spacing
    
    node_positions[comp["name"]] = {
        "x": x_pos,
        "y": layer_y[layer],
        "color": comp["color"],
        "type": comp["type"]
    }
    layer_indices[layer] += 1

# Create the figure
fig = go.Figure()

# Define connection colors based on type (blue for AI, green for validation, red for error, orange for cache)
line_colors = {
    "data": "#1FB8CD",        # Blue for data flow
    "ai": "#1FB8CD",          # Blue for AI processing
    "validation": "#5D878F",   # Green for validation
    "error": "#DB4545",       # Red for error handling
    "cache": "#FFC185",       # Orange for caching
    "storage": "#FFC185",     # Orange for storage
    "feedback": "#D2BA4C"     # Yellow for feedback
}

# Add connections first (so they appear behind nodes)
for conn in data["connections"]:
    from_pos = node_positions[conn["from"]]
    to_pos = node_positions[conn["to"]]
    
    line_color = line_colors.get(conn["type"], "#666666")
    
    # Calculate connection path with better arrow positioning
    dx = to_pos["x"] - from_pos["x"]
    dy = to_pos["y"] - from_pos["y"]
    
    # Adjust start and end points to avoid overlapping with nodes
    node_radius = 0.3
    length = np.sqrt(dx**2 + dy**2)
    if length > 0:
        dx_norm = dx / length
        dy_norm = dy / length
        
        start_x = from_pos["x"] + dx_norm * node_radius
        start_y = from_pos["y"] + dy_norm * node_radius
        end_x = to_pos["x"] - dx_norm * node_radius
        end_y = to_pos["y"] - dy_norm * node_radius
        
        # Add connection line
        fig.add_trace(go.Scatter(
            x=[start_x, end_x],
            y=[start_y, end_y],
            mode='lines',
            line=dict(color=line_color, width=3),
            showlegend=False,
            hoverinfo='skip'
        ))
        
        # Add arrowhead
        arrow_length = 0.15
        arrow_x = end_x - dx_norm * arrow_length
        arrow_y = end_y - dy_norm * arrow_length
        
        # Arrow sides
        perp_x = -dy_norm * arrow_length * 0.6
        perp_y = dx_norm * arrow_length * 0.6
        
        fig.add_trace(go.Scatter(
            x=[arrow_x + perp_x, end_x, arrow_x - perp_x, arrow_x + perp_x],
            y=[arrow_y + perp_y, end_y, arrow_y - perp_y, arrow_y + perp_y],
            mode='lines',
            line=dict(color=line_color, width=2),
            fill='toself',
            fillcolor=line_color,
            showlegend=False,
            hoverinfo='skip'
        ))

# Add nodes with larger size and better text
for comp in data["components"]:
    pos = node_positions[comp["name"]]
    
    # Use full name but format for better display
    display_name = comp["name"]
    if len(display_name) > 12:
        words = display_name.split()
        if len(words) > 1:
            mid = len(words) // 2
            display_name = "<br>".join([" ".join(words[:mid]), " ".join(words[mid:])])
    
    fig.add_trace(go.Scatter(
        x=[pos["x"]],
        y=[pos["y"]],
        mode='markers+text',
        marker=dict(
            size=40,  # Increased size
            color=pos["color"],
            line=dict(width=3, color='#333333'),
            opacity=0.9
        ),
        text=display_name,
        textposition="middle center",
        textfont=dict(size=11, color='#333333', family="Arial Black"),
        showlegend=False,
        hovertemplate=f"<b>{comp['name']}</b><br>Layer: {comp['layer']}<br>Type: {comp['type']}<extra></extra>"
    ))

# Add layer labels with better positioning
for layer, y_pos in layer_y.items():
    fig.add_trace(go.Scatter(
        x=[-8],  # Moved further left
        y=[y_pos],
        mode='text',
        text=f"<b>{layer} Layer</b>",
        textfont=dict(size=16, color='#333333', family="Arial"),
        showlegend=False,
        hoverinfo='skip'
    ))

# Add legend for connection types
legend_x = 6
legend_y_start = len(layer_order) * 2 - 1
legend_items = [
    ("Data Flow", "#1FB8CD"),
    ("AI Processing", "#1FB8CD"), 
    ("Validation", "#5D878F"),
    ("Error Handling", "#DB4545"),
    ("Caching/Storage", "#FFC185"),
    ("Feedback", "#D2BA4C")
]

for i, (label, color) in enumerate(legend_items):
    fig.add_trace(go.Scatter(
        x=[legend_x, legend_x + 0.5],
        y=[legend_y_start - i * 0.7, legend_y_start - i * 0.7],
        mode='lines+text',
        line=dict(color=color, width=4),
        text=[None, label],
        textposition="middle right",
        textfont=dict(size=12, color='#333333'),
        showlegend=False,
        hoverinfo='skip'
    ))

# Add legend title
fig.add_trace(go.Scatter(
    x=[legend_x + 0.25],
    y=[legend_y_start + 0.5],
    mode='text',
    text="<b>Connection Types</b>",
    textfont=dict(size=14, color='#333333'),
    showlegend=False,
    hoverinfo='skip'
))

# Update layout
fig.update_layout(
    title="Enhanced NLP Processor Architecture",
    xaxis=dict(
        showgrid=False,
        showticklabels=False,
        zeroline=False,
        range=[-10, 10]
    ),
    yaxis=dict(
        showgrid=False,
        showticklabels=False,
        zeroline=False,
        range=[-1, len(layer_order) * 2]
    ),
    plot_bgcolor='white',
    paper_bgcolor='white'
)

# Save the figure
fig.write_image("nlp_architecture_diagram.png", width=1400, height=1000, scale=2)

print("Enhanced architecture diagram created successfully!")