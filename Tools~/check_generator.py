"""Verify the bundled Unity compiler plugin matches its public source."""
from pathlib import Path
import subprocess
package = Path(__file__).resolve().parents[1]
project = package / 'Tools~/Pine.Generator/Pine.Generator.csproj'
subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '--verbosity', 'quiet'], check=True)
built = project.parent / 'bin/Release/netstandard2.0/Pine.Generator.dll'
assert built.read_bytes() == (package / 'Runtime/Compiler/Pine.Generator.dll').read_bytes(), 'Bundled compiler plugin differs from source'
print('Pine compiler plugin matches source.')
