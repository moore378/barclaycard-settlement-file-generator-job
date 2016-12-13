import sys
import os
import re

from contextlib import contextmanager
import subprocess # just to call an arbitrary command e.g. 'ls'

@contextmanager
def cd(newdir):
    prevdir = os.getcwd()
    os.chdir(os.path.expanduser(newdir))
    try:
        yield
    finally:
        os.chdir(prevdir)

solution        = ""
platform        = ""
configuration   = "Debug"
component       = "all"

projects = [] # empty array

length = len(sys.argv)
if length > 1:
    configuration = sys.argv[1]
if length > 2:
    # Allow the building of only one project
    component = sys.argv[2].lower()

print( "Using configuration {} and component list of {}".format( configuration, component ) )

for fn in open( 'make-files.txt', 'r'):
    fn = fn.strip()

    # variable?
    match = re.match(r"^(.+?)=(.+)$", fn)
    if match:
        variableName = match.group(1)
        args = match.group(2)
        if variableName == "solution":
            solution = args
        elif variableName == "platform":
            platform = args
    else:
        # These are the projects to package.
        projects.append( fn.lower() ) # convert to lower case

# First restore any packages
subprocess.call( "nuget restore" )

# Build the solution
exec  = "msbuild " + solution + " /t:Clean;Rebuild /m /p:DeployOnBuild=true /p:PublishProfile=Local.pubxml /p:\"Platform=" + platform + "\" /p:Configuration=" + configuration + "\""
#print( exec )
rc = subprocess.call( exec )

# If for some reason the build fails, error out!
if (0 != rc):
   sys.exit(1)

# Get the directory for traversal of each project.
dirname = os.path.dirname(solution)
if ("" == dirname):
    dirname = "."

for entry in projects:
    if ((entry == component) or component == "all"):
        directory = dirname + "\\" + entry + "\\bin\\" + configuration
        with cd(directory):
            # we are in the binary output directory           
            subprocess.call("..\\..\\..\\scripts\make-deploy.py", shell=True)

