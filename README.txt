WorkerDistributionSystem - Assignment Solution

Components:
1. WorkerDistributionSystem.Domain
2. WorkerDistributionSystem.Application  
3. WorkerDistributionSystem.Infrastructure
4. WorkerDistributionSystem.WindowsService
5. WorkerDistributionSystem.AdminCLI
6. WorkerDistributionSystem.WorkerApp

Build Instructions:
1. Open WorkerDistributionSystem.sln in Visual Studio
2. Build ? Rebuild Solution
3. Install Windows Service:
   - Run Command Prompt as Administrator
   - Navigate to WindowsService\bin\Debug\net8.0\
   - sc create WorkerDistributionService binPath= "[full_path_to_exe]"
   - sc start WorkerDistributionService

Usage:
- AdminCLI: worker execute "command", service start/stop
- WorkerApp: Run to connect as worker
- Service: Manages workers and tasks

