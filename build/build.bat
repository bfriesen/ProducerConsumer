:: TODO: Increment the build number in both AssemblyInfo.cs and the .nuspec files.

%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe ..\src\ProducerConsumer.sln /t:rebuild /verbosity:quiet /logger:FileLogger,Microsoft.Build.Engine; /p:Configuration=Release /p:Platform="Any CPU"

explorer ..\nuget