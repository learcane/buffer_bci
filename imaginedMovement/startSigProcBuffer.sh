#!/bin/bash
cd `dirname ${BASH_SOURCE[0]}`
source ../utilities/findMatlab.sh
if [[ $matexe == *matlab ]]; then  args=-nodesktop; fi
cat <<EOF | $matexe $args
configureIM;
startSigProcBuffer('epochEventType','stimulus.target','freqband',[6 8 28 30],'clsfr_type','ersp','trlen_ms',trlen_ms);
%quit;
EOF
