REM cleanup previous models so only new models remain. other files get overwritten anyway
del /s .\code\output_files\models\*

REM run train_c2k.py
python .\code\train_c2k.py

REM get latest modelfile
for /f "tokens=*" %%a in ('dir .\code\output_files\models /b /od') do set newest=%%a

REM run evaluations
python .\code\evaluate_next_activity_and_time_c2k.py %newest%
python .\code\evaluate_suffix_and_remaining_time_c2k.py %newest%
python .\code\calculate_accuracy_on_next_event_c2k.py

pause