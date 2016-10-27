#!/bin/sh

# Carlos Sanchez - 2016
# randomouscrap98@aol.com

# This script performs a git add --all, a git commit with
# the first argument to the script as the message, and a
# git push all in one. I don't usually care about the 
# individual steps, so this makes it easier.

if [ $# -ne 1 ]
then
   echo "Needs one argument. Example:"
   echo "./`basename $0` \"My commit message\""
   exit 1
fi

echo "Autogit starting..."
git add --all
git commit -m "$1"
git pull --rebase
git push
echo "Autogit complete!"

exit 0
