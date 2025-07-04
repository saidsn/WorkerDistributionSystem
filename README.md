Worker Distribution System 🚀

📋 Project Overview

Worker Distribution System is a robust C# .NET application designed to distribute workloads across multiple worker processes using Clean Architecture principles. The system consists of a Windows Service, multiple Worker applications, and an Administrative CLI for managing the entire infrastructure.
Bu sistem Clean Architecture prinsiplərinə əsasən hazırlanmış, distributed workload management sistemidir. Sistem Windows Service, Worker applications və Administrative CLI-dan ibarətdir.

🏗️ Architecture

	┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
	│   Admin CLI     │    │ Windows Service │    │   Worker Apps   │
	│                 │    │                 │    │                 │
	│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
	│ │   Commands  │ │◄──►│ │ TCP Server  │ │◄──►│ │ TCP Client  │ │
	│ │             │ │    │ │             │ │    │ │             │ │
	│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
	│                 │    │                 │    │                 │
	│                 │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
	│                 │    │ │Task Manager │ │    │ │   Executor  │ │
	│                 │    │ │             │ │    │ │             │ │
	│                 │    │ └─────────────┘ │    │ └─────────────┘ │
	└─────────────────┘    └─────────────────┘    └─────────────────┘

Clean Architecture Layers:

- Domain Layer: Entities, Enums, Core business logic
- Application Layer: Services, DTOs, Business rules
- Infrastructure Layer: Repositories, TCP Communication, Data access
- Presentation Layer: CLI Interface, Windows Service

📋 Prerequisites
- Operating System: Windows 10 or Windows 11
- .NET: .NET 8.0 SDK or later
- IDE: Visual Studio 2022, JetBrains Rider, or VS Code
- Development Environment: Windows environment for testing

🚀 Installation & Setup

1. Clone the Repository

	git clone <repository-url>
	cd WorkerDistributionSystem
	
	# Build Admin CLI
	cd ../WorkerDistributionSystem.AdminCLI
	dotnet build

2. Run the Applications

	# Run Admin CLI
	cd WorkerDistributionSystem.AdminCLI
	dotnet run

🎮 Usage

Starting the System

1. Start Admin CLI:

	cd WorkerDistributionSystem.AdminCLI
	dotnet run

1. Start Windows Service:

	> service start


1. Add Workers:

	> worker add worker1
	> worker add worker2


1. Execute Commands:

	> worker execute "whoami"
	> worker execute "dir"
	> worker execute "ipconfig"

📝 Available Commands

Service Commands

	service start        # Start the Windows Service
	service stop         # Stop the Windows Service and all workers
	service status       # Show service status and connected workers

Worker Commands

	worker add <name>           # Add a new worker instance
	worker remove <name>        # Remove a specific worker
	worker remove all          # Remove all workers
	worker status              # Show status of all workers
	worker status <name>       # Show status of specific worker
	worker execute "<command>" # Execute command on available worker

General Commands

	help                # Show available commands
	clear / cls         # Clear console
	exit / quit         # Exit the application

📂 Project Structure

	WorkerDistributionSystem/
	├── WorkerDistributionSystem.Domain/
	│   ├── Entities/
	│   │   ├── Worker.cs
	│   │   ├── WorkerTask.cs
	│   │   └── ServiceStatus.cs
	│   └── Enums/
	│       ├── WorkerStatus.cs
	│       └── WorkerTaskStatus.cs
	├── WorkerDistributionSystem.Application/
	│   ├── Services/
	│   │   ├── Interfaces/
	│   │   └── Implementations/
	│   └── DTOs/
	├── WorkerDistributionSystem.Infrastructure/
	│   └── Repositories/
	│       ├── Interfaces/
	│       └── Implementations/
	├── WorkerDistributionSystem.WindowsService/
	│   ├── Program.cs
	│   └── WorkerDistributionBackgroundService.cs
	├── WorkerDistributionSystem.WorkerApp/
	│   └── Program.cs
	├── WorkerDistributionSystem.AdminCLI/
	│   ├── Program.cs
	│   └── Commands/
	│       ├── ICommandProcessor.cs
	│       └── CommandProcessor.cs
	└── README.md

⚙️ Configuration

TCP Communication

- Port: 8080 (default)
- Protocol: TCP
- Host: localhost (127.0.0.1)

Worker Settings

- Heartbeat Interval: 30 seconds
- Task Timeout: 30 seconds
- Connection Timeout: 10 seconds

🔍 Example Usage Session

	Worker Distribution System - Admin CLI
	Usage: AdminCLI <command> [options]
	
	Commands:
	  worker execute "<command>"  - Execute command on available worker
	  worker add <worker_name>     - Add new worker instance
	  worker remove <worker_name>  - Remove worker instance
	  worker status                - Show workers status
	  service start                - Start the Windows service
	  service stop                 - Stop the Windows service
	  service status               - Show service status
	<-------------------------------------------------------------------->
	
	> service start
	Windows Service started successfully (PID: 12345)
	
	> worker add worker1
	Worker 'worker1' started successfully (PID: 67890)
	
	> worker add worker2
	Worker 'worker2' started successfully (PID: 54321)
	
	> worker status
	Workers Status:
	  - worker1 (PID: 67890) - RUNNING
	  - worker2 (PID: 54321) - RUNNING
	
	> worker execute "whoami"
	Task queued successfully. Waiting for result...
	Command Result: desktop-xyz\username
	
	> worker execute "dir"
	Task queued successfully. Waiting for result...
	Command Result: [Directory listing output]
	
	> service status
	SERVICE STATUS: RUNNING
	Service PID: 12345
	Connected Workers: 2
	  - worker1 (PID: 67890) - RUNNING
	  - worker2 (PID: 54321) - RUNNING
	
	> service stop
	Stopping Windows Service and all workers...
	All workers removed successfully
	Windows Service stopped successfully

📄 Architecture Details

Communication Flow

1. Admin CLI → TCP Client → Windows Service
2. Windows Service → Task Queue → Worker Selection
3. Windows Service → TCP Server → Worker Apps
4. Worker Apps → Command Execution → Result
5. Worker Apps → TCP Client → Windows Service
6. Windows Service → TCP Server → Admin CLI