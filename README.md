# SmartGrid - Linux Setup

This project demonstrates a high-performance **SmartGridView** control using **Uno Platform** (WinUI 3 on Linux).

## Quick Start

You can run the application directly using the helper script:

```bash
./run.sh
```

## Detailed Setup Instructions

The project has been configured to specific Linux environment constraints.

1.  **Project Location**:
    The main project entry point is `SmartGridApp/SmartGridApp/SmartGridApp.Manual.csproj`.
    *Note: We use this manual project file instead of the standard `SmartGridApp.csproj` to bypass dependency issues with the default Uno Sdk templates on this environment.*

2.  **Running Manually**:
    If you prefer to run the commands yourself:
    ```bash
    cd SmartGridApp/SmartGridApp
    dotnet restore SmartGridApp.Manual.csproj
    dotnet run --project SmartGridApp.Manual.csproj
    ```

## Features Implemented

*   **SmartGridView Control**: A custom implementation of a grid view supporting variable sized items.
*   **Virtualization**: Uses `FastCardLayout` (VirtualizingLayout) for high performance.
*   **Drag & Drop**: Logic for drag and drop reordering is implemented (Note: full visual feedback depends on platform support, currently basic structure is in place).
*   **Linux (GTK)**: Runs on Linux via Uno Platform's Skia/GTK head.

## Troubleshooting

If you encounter "Resizetizer" errors or "Runtime directory not found", ensure you use the `./run.sh` script or target the `.Manual.csproj` as shown above, which deliberately excludes those problematic build tools.
