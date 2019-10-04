#!/usr/bin/env bash

TEST_RESULT="passed"
COUNTER=0

#=== start functions ===

# runs all tests and logs the run for later use
function run_all_tests {
    dotnet test ../src/IO.Ably.Tests.DotNetCore20/IO.Ably.Tests.DotNetCore20.csproj --logger 'trx;logfilename=test-results.trx'
}

# function to run failed tests
# requires that run_all_tests has already been called first
function run_failed_tests {
	# called in a loop, so reset TEST_RESULT
	TEST_RESULT="passed"
	# extract the failed test names from the test log into a text file
	grep 'outcome="Failed"' ../src/IO.Ably.Tests.DotNetCore20/TestResults/test-results.trx | sed -n 's/.*testName="\([^"]*\).*/\1/p' > ../src/IO.Ably.Tests.DotNetCore20/TestResults/failed.txt
	# read the file line by line into t
    while read t; do
    	echo "* RETRYING FAILED TEST $t"
    	# check $t is not empty
    	if [ ! -z "$t" ]; then
    		# We need the FQN, the test log can include the parameters, so remove them
    		t_clean=$(echo "$t" | sed -e 's/(.*//g')
    		# run the failed test again
			dotnet test ../src/IO.Ably.Tests.DotNetCore20/IO.Ably.Tests.DotNetCore20.csproj --filter "$t_clean"
			if [ $? -eq 1 ]; then
	    		echo "* RETRIED TEST FAILED: $t_clean" 			
				TEST_RESULT="failed"
			fi
		fi
	done <../src/IO.Ably.Tests.DotNetCore20/TestResults/failed.txt
}

#=== end functions ===

# run all tests and log the run
run_all_tests
# check if there was an error indicating 1 or more tests failed
# if there were failures re-run the failed tests
if [ $? -eq 0 ]; then
	echo "* TESTS PASSED"
	COUNTER=99
fi

echo "TEST RESULT: $TEST_RESULT"
# if the tests didn't pass retry the failed tests upto 3 times
while [  $COUNTER -lt 3 ]; do
	run_failed_tests
	let COUNTER=COUNTER+1
	if [ $TEST_RESULT == "passed" ]; then COUNTER=99; fi
done

# set an exit code fro the ci server to pick up
if [ $TEST_RESULT == "passed" ]; then
	echo "* TESTS PASSED"
	exit 0
else	
	echo "* TESTS FAILED"
	exit 1
fi


