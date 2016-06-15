REM Register Service Namespaces for all Authorization Processors.

REM netsh http delete urlacl https://+:56341/israelprocessor
REM netsh http delete urlacl https://+:56341/AuthorizationProcessors/Fis-PayDirect
REM netsh http delete urlacl https://+:56341/AuthorizationProcessors/BarclaysTns
netsh http add urlacl url=https://+:56341/israelprocessor sddl=D:(A;;GX;;;S-1-1-0)
netsh http add urlacl url=https://+:56341/AuthorizationProcessors/Fis-PayDirect sddl=D:(A;;GX;;;S-1-1-0)
REM netsh http add urlacl url=https://+:56341/AuthorizationProcessors/BarclaysTns sddl=D:(A;;GX;;;S-1-1-0)
