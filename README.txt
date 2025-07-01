💼 WorkerDistributionSystem — Assignment Solution
🧩 Components
The solution is organized into the following projects:

WorkerDistributionSystem.Domain – Core domain entities and enums

WorkerDistributionSystem.Application – Business logic & service interfaces

WorkerDistributionSystem.Infrastructure – Repositories, communication, and data access

WorkerDistributionSystem.WindowsService – Background task manager (runs as a Windows Service)

WorkerDistributionSystem.AdminCLI – Command Line Interface for controlling and monitoring

WorkerDistributionSystem.WorkerApp – Simulates a connected worker that receives and executes tasks

⚙️ Build Instructions
Open the solution file WorkerDistributionSystem.sln in Visual Studio

From the menu, select:

Build > Rebuild Solution

🛠️ Installing the Windows Service
📌 Make sure to run Command Prompt as Administrator

Navigate to the output directory of the service:

cd WorkerDistributionSystem.WindowsService\bin\Debug\net8.0\
Install the service using sc:

sc create WorkerDistributionService binPath= "C:\Full\Path\To\WorkerDistributionSystem.WindowsService.exe"
Start the service:

sc start WorkerDistributionService
🚀 Usage
🔧 AdminCLI (Command Line Control)
Run commands via AdminCLI:

# Execute a command on available worker
worker execute "dir"

# Add or remove a worker
worker add MyWorker

worker remove <worker-id>

# Check worker status
worker status

worker status <worker-id>

# Manage the service
service start

service stop

service status

🧑‍💻 WorkerApp (Simulated Worker)
To start a worker:

WorkerApp.exe [optional-worker-name]
Each worker connects to the central TCP server and listens for task assignments.

🖥️ WindowsService (Core Dispatcher)
This background service:

Accepts and manages worker connections

Distributes queued tasks to idle workers

Updates task statuses upon completion

✅ Example Workflow
Start Windows Service
→ via AdminCLI or sc start WorkerDistributionService

Launch WorkerApp(s)
→ connects and registers workers

Run AdminCLI Commands
→ assign commands like dir, ping, ipconfig, etc.

Monitor status
→ via worker status or service status

