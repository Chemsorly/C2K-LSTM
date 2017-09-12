REM create folders if not exist (e.g. after git clone)
if not exist .\output_files\folds\ mkdir .\output_files\folds
if not exist .\output_files\models\ mkdir .\output_files\models
if not exist .\output_files\results\ mkdir .\output_files\results

REM cleanup previous models so only new models remain. other files get overwritten anyway
del /s .\output_files\models\*

REM run train_c2k.py
python train_c2k.py

REM get latest modelfile
for /f "tokens=*" %%a in ('dir .\output_files\models /b /od') do set newest=%%a

REM run evaluations
python evaluate_next_activity_and_time_c2k.py %newest%
python evaluate_suffix_and_remaining_time_c2k.py %newest%
python calculate_accuracy_on_next_event_c2k.py

pause