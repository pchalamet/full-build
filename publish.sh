#! /bin/bash

function failure
{
  exit 5
}

mono dotnet/fullbuild/bin/fullbuild.exe publish "*" || failure

