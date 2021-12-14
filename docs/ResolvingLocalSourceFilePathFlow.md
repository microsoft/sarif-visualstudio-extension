# Resolving SARIF results artifact file location

### Here is the end to end user flow for resolving source file of SARIF result from local enlistment.

- Does the SARIF contain embedded file content?

    - YES: ok, can we match the file path to alternate content on disk?

        - Yes: ok, does the disk based file hash match the embedded content hash?

            - Yes: [OPEN THE FILE FROM DISK]

            - No: [SHOW THE EMBEDDED CONTENT VS. DISK CONTENT DIALOG]

                - User select the embedded content: [SHOW THE EMBEDDED FILE CONTENTS]

                - User select the file from disk: [OPEN THE FILE FROM DISK]

                - User select to browse the alternate file: [SHOW OPEN FILE DIALOG AND OPEN USER SELECTED FILE]

        - No: [SHOW THE EMBEDDED FILE CONTENTS]

    - No: ok, does the SARIF contain file hashes?

        - Yes: ok, can we match the file path to alternate content on disk?

            - Yes: ok, does the file hash match the log file hash?

                - Yes: [OPEN THE FILE FROM DISK]

                - No: [SHOW THE ‘MATCHED YOUR FILE BUT IT’S DIFFERENT’ DIALOG]

                    - User select the file from disk: [OPEN THE FILE FROM DISK]

                    - User select to browse the alternate file: [SHOW OPEN FILE DIALOG AND OPEN USER SELECTED FILE]

            - No: [SHOW THE ‘WE CAN’T FIND A FILE FOR YOU, CAN YOU BROWSE TO IT?’ DIALOG]

        - No: ok, can we match the file path to alternate content on disk?

            - Yes: [OPEN THE FILE FROM DISK] 

            - No: [SHOW THE ‘WE CAN’T FIND A FILE FOR YOU, CAN YOU BROWSE TO IT?’ DIALOG]

### Test case matrix based on abvoe flow:

| Has Embedded file | Has Hash | Has local file | Hash codes match | Expected Action |
| --- | --- | --- | --- | --- |
| Yes |	Yes | Yes | Yes | Open local file automatically w/o dialog |
| Yes | Yes | Yes | No | Open dialog with 3 options:  [Open embedded file] / [Open local file] / [Browse alternate location] |
| Yes | Yes | No | N/A | Open embedded file w/o dialog |
| No | Yes | Yes | Yes | Open local file automatically w/o dialog |
| No | Yes |Yes | No | Open dialog with 2 options:  [Open local file] / [Browse alternate location] |
| No | Yes | No | N/A | Open file dialog |
| No | No | Yes | N/A | Open local file automatically w/o dialog |
| No | No | No | N/A | Open file dialog |

