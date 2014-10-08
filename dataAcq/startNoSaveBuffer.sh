#!/bin/bash
buffdir=`dirname $0`
# Identify the OS and search for the appropriate executable
if [ `uname -s` == 'Linux' ]; then
	 if  [ "`uname -a`" == 'armv6l' ]; then
		  arch='raspberrypi'
    else
		  arch='glnx86';
   fi
   buffexe=$buffdir'/buffer/bin/demo_buffer';
   if [ -r $buffdir/demo_buffer ]; then
    buffexe=$buffdir'/demo_buffer';
   fi
   if [ -r $buffdir/buffer/bin/${arch}/demo_buffer ]; then
	 buffexe=$buffdir'/buffer/bin/${arch}/demo_buffer';
   fi
   if [ -r $buffdir/buffer/${arch}/demo_buffer ]; then
	 buffexe=$buffdir'/buffer/${arch}/demo_buffer';
   fi
elif [[ `uname -s` = 'MINGW'* ]]; then
	 arch='win32'
	 buffexe=$bufdir'/buffer/win32/demo_buffer_unix'
else # Mac
	arch='maci';
fi
# Search for the exec in the standard places
if [ -r $buffdir/buffer/bin/${arch}/buffer ]; then
	 buffexe=$buffdir"/buffer/bin/${arch}/buffer";
fi
if [ -r $buffdir/buffer/${arch}/buffer ]; then
	 buffexe=$buffdir"/buffer/${arch}/buffer";
fi
echo Starting: $buffexe
$buffexe