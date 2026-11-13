"""Hash the retained resolved pre-Csc observations without claiming task interception."""
from pathlib import Path
from producer_common import sha
KINDS={'Compile','AdditionalFile','Analyzer','Reference','Resource','EmbeddedFile','LinkResource','Module','AnalyzerConfig','SourceLink','Win32Resource','Win32Manifest','Win32Icon','KeyFile','CompilerAppConfig','CodeAnalysisRules','ResponseFile'}
def read(directory):
 directory=Path(directory);manifest=directory/'story65-domain-compiler-inputs.txt';rows=[]
 for line in manifest.read_text(encoding='utf-8-sig').splitlines():
  parts=line.split('|');assert len(parts)>=2 and parts[0]in KINDS;file=Path(parts[1]);assert file.is_absolute()and file.is_file()and not file.is_symlink()
  rows.append({'kind':parts[0],'path':str(file),'sha256':sha(file),'itemMetadata':parts[2:]})
 writes=directory/'story65-domain-writable-paths.txt';paths=writes.read_text(encoding='utf-8-sig').splitlines()
 pre=directory/'story65-domain-precore-flags.txt';post=directory/'story65-domain-postcore-flags.txt'
 def flags(file):
  values={}
  for line in file.read_text(encoding='utf-8-sig').splitlines():
   key,separator,value=line.partition('|');assert separator;values.setdefault(key,[]).append(value)
  return values
 return{'observedInputs':rows,'preCoreCompileFlags':flags(pre),'postCoreCompileFlags':flags(post),'observedWritePaths':paths,'manifestsSha256':{p.name:sha(p)for p in[manifest,writes,pre,post]},'pinnedCscParameterAndTargetOrderProofRequired':True,'notTaskInterceptionFullCompilerCommandLineOrByteEquivalence':True,'msbuildGeneratedSourcesIncludedButUnemittedRoslynGeneratedOutputsNotEnumerated':True}
