'''
this script trains an LSTM model on one of the data files in the data folder of
this repository. the input file can be changed to another file from the data folder
by changing its name in line 46.

it is recommended to run this script on GPU, as recurrent networks are quite 
computationally intensive.

Author: Niek Tax
'''

from __future__ import print_function, division
from keras.models import Sequential, Model
from keras.layers.core import Dense
from keras.layers.recurrent import LSTM, GRU, SimpleRNN
from keras.layers import Input
from keras.utils.data_utils import get_file
from keras.optimizers import Nadam
from keras import optimizers
from keras.callbacks import EarlyStopping, ModelCheckpoint, ReduceLROnPlateau
from keras.layers.normalization import BatchNormalization
from collections import Counter
import unicodecsv
import numpy as np
import random
import sys
import os
import copy
import csv
import time
import shutil
from itertools import izip
from datetime import datetime
from math import log

#parameters
#int architecture: 1,2,3
par_architecture = ""

#int neurons: 50,100,150,200
par_neurons = ""

#double dropout: 0.2, 0.4, 0.6, 0.8
par_dropout = ""

#int patience: 20, 40, 60, 80
par_patience = ""

#int optimizing algorithm: ~7
par_algorithm = ""

#get args
if __name__ == "__main__":
    par_architecture = int(sys.argv[1])
    par_neurons = int(sys.argv[2])
    par_dropout = float(sys.argv[3])
    par_patience = int(sys.argv[4])
    par_algorithm = int(sys.argv[5])

# create folder is not exist
if not os.path.exists('output_files/folds'):
    os.makedirs('output_files/folds')
if not os.path.exists('output_files/models'):
    os.makedirs('output_files/models')
if not os.path.exists('output_files/results'):
    os.makedirs('output_files/results')

#clear folders
shutil.rmtree('output_files/folds')
os.makedirs('output_files/folds')
shutil.rmtree('output_files/models')
os.makedirs('output_files/models')
shutil.rmtree('output_files/results')
os.makedirs('output_files/results')        

lastcase = ''
line = [] #needs to be an array now for rgb encoding
firstLine = True
lines = []
timeseqs = []
timeseqs2 = []
times = []
times2 = []
numlines = 0
casestarttime = None
lasteventtime = None
eventlog = "c2k_data_comma_lstmready_multi.csv"

csvfile = open('../data/%s' % eventlog, 'r')
spamreader = csv.reader(csvfile, delimiter=',', quotechar='|')
next(spamreader, None)  # skip the headers

for row in spamreader:
    t = int(row[2])
    if row[0]!=lastcase:
        casestarttime = t
        lasteventtime = t
        lastcase = row[0]
        if not firstLine:        
            lines.append(line)
            timeseqs.append(times)
            timeseqs2.append(times2)
        line = []
        times = []
        times2 = []
        numlines+=1
    line.append(row[1])
    timesincelastevent = int(row[3]) #3 is calculated time since last event
    timesincecasestart = int(row[4]) #4 is timestamp aka time since case start
    timediff = timesincelastevent
    timediff2 = timesincecasestart
    times.append(timediff)
    times2.append(timediff2)
    lasteventtime = t
    firstLine = False

# add last case
lines.append(line)
timeseqs.append(times)
timeseqs2.append(times2)
numlines+=1

## generate lstm variables
elems_per_fold = int(round(numlines/3)) #divide data in 3 parts. 1-2 are train data, 3 validation data for later (not used here)
fold1 = lines[:elems_per_fold]
fold1_t = timeseqs[:elems_per_fold]
fold1_t2 = timeseqs2[:elems_per_fold]

fold2 = lines[elems_per_fold:2*elems_per_fold]
fold2_t = timeseqs[elems_per_fold:2*elems_per_fold]
fold2_t2 = timeseqs2[elems_per_fold:2*elems_per_fold]

fold3 = lines[2*elems_per_fold:]
fold3_t = timeseqs[2*elems_per_fold:]
fold3_t2 = timeseqs2[2*elems_per_fold:]

lines = fold1 + fold2
lines_t = fold1_t + fold2_t
lines_t2 = fold1_t2 + fold2_t2

step = 1
sentences = []
softness = 0
next_chars = []
lines = map(lambda x: x + ['!'],lines)

maxlen = max(map(lambda x: len(x),lines)) #variable for lstm model

#chars are concurrent activities e.g. 'AEI'
#uniquechars are activities e.g. 'A'
chars = map(lambda x : set(x),lines)
chars = list(set().union(*chars))
chars.sort()
target_chars = copy.copy(chars)
chars.remove('!')
print('total chars: {}, target chars: {}'.format(len(chars), len(target_chars)))

uniquechars = [l for word in chars for l in word]
uniquechars.append('!')
uniquechars = list(set(uniquechars))
uniquechars.sort()
target_uchars = copy.copy(uniquechars)
uniquechars.remove('!')
print('unique characters: {}', uniquechars)

char_indices = dict((c, i) for i, c in enumerate(chars)) #dictionary<key,value> with <char, index> where char is unique symbol for activity
uchar_indices = dict((c, i) for i, c in enumerate(uniquechars))
indices_char = dict((i, c) for i, c in enumerate(chars)) #dictionary<key,value> with <index, char> where char is unique symbol for activity
indices_uchar = dict((i, c) for i, c in enumerate(uniquechars))

target_char_indices = dict((c, i) for i, c in enumerate(target_chars))
target_uchar_indices = dict((c, i) for i, c in enumerate(target_uchars))
target_indices_char = dict((i, c) for i, c in enumerate(target_chars))
target_indices_uchar = dict((i, c) for i, c in enumerate(target_uchars))

print(char_indices)
print(indices_char)
print(target_char_indices) #does contain '!'
print(target_indices_char) #does contain '!'

print(uchar_indices)
print(indices_uchar)
print(target_uchar_indices) #does contain '!'
print(target_indices_uchar) #does contain '!'

## end variables

## prepare input matrix
csvfile = open('../data/%s' % eventlog, 'r')
spamreader = csv.reader(csvfile, delimiter=',', quotechar='|')
next(spamreader, None)  # skip the headers
lastcase = ''
line = []
firstLine = True
lines = []
timeseqs = []
timeseqs2 = []
timeseqs3 = []
times = []
times2 = []
times3 = []
numlines = 0
casestarttime = None
lasteventtime = None
for row in spamreader:
    t = int(row[2])
    if row[0]!=lastcase:
        casestarttime = t
        lasteventtime = t
        lastcase = row[0]
        if not firstLine:        
            lines.append(line)
            timeseqs.append(times)
            timeseqs2.append(times2)
            timeseqs3.append(times3)
        line = []
        times = []
        times2 = []
        times3 = []
        numlines+=1
    #line+=row[1]
    line.append(row[1])
    timesincelastevent = int(row[3]) #4 is calculated time since last event
    timesincecasestart = int(row[4]) #5 is timestamp aka time since case start
    timediff = timesincelastevent
    timediff2 = timesincecasestart
    timediff3 = int(row[2]) 
    times.append(timediff)
    times2.append(timediff2)
    times3.append(timediff3)
    lasteventtime = t
    firstLine = False

# add last case
lines.append(line)
timeseqs.append(times)
timeseqs2.append(times2)
timeseqs3.append(times3)
numlines+=1

divisor = np.mean([item for sublist in timeseqs for item in sublist]) #variable for lstm model
print('divisor: {}'.format(divisor))
divisor2 = np.mean([item for sublist in timeseqs2 for item in sublist]) #variable for lstm model
print('divisor2: {}'.format(divisor2))
divisor3 = np.mean([item for sublist in timeseqs3 for item in sublist]) #variable for lstm model
print('divisor3: {}'.format(divisor3))

elems_per_fold = int(round(numlines/3))
fold1 = lines[:elems_per_fold]
fold1_t = timeseqs[:elems_per_fold]
fold1_t2 = timeseqs2[:elems_per_fold]
fold1_t3 = timeseqs3[:elems_per_fold]
with open('output_files/folds/fold1.csv', 'wb') as csvfile:
    spamwriter = csv.writer(csvfile, delimiter=',', quotechar='|', quoting=csv.QUOTE_MINIMAL)
    for row, timeseq in izip(fold1, fold1_t):    
        spamwriter.writerow([unicode(s).encode("utf-8") +'#{}'.format(t) for s, t in izip(row, timeseq)])

fold2 = lines[elems_per_fold:2*elems_per_fold]
fold2_t = timeseqs[elems_per_fold:2*elems_per_fold]
fold2_t2 = timeseqs2[elems_per_fold:2*elems_per_fold]
fold2_t3 = timeseqs3[elems_per_fold:2*elems_per_fold]
with open('output_files/folds/fold2.csv', 'wb') as csvfile:
    spamwriter = csv.writer(csvfile, delimiter=',', quotechar='|', quoting=csv.QUOTE_MINIMAL)
    for row, timeseq in izip(fold2, fold2_t):
        spamwriter.writerow([unicode(s).encode("utf-8") +'#{}'.format(t) for s, t in izip(row, timeseq)])
        
fold3 = lines[2*elems_per_fold:]
fold3_t = timeseqs[2*elems_per_fold:]
fold3_t2 = timeseqs2[2*elems_per_fold:]
fold3_t3 = timeseqs3[2*elems_per_fold:]
with open('output_files/folds/fold3.csv', 'wb') as csvfile:
    spamwriter = csv.writer(csvfile, delimiter=',', quotechar='|', quoting=csv.QUOTE_MINIMAL)
    for row, timeseq in izip(fold3, fold3_t):
        spamwriter.writerow([unicode(s).encode("utf-8") +'#{}'.format(t) for s, t in izip(row, timeseq)])

lines = fold1 + fold2
lines_t = fold1_t + fold2_t
lines_t2 = fold1_t2 + fold2_t2
lines_t3 = fold1_t3 + fold2_t3

step = 1
sentences = []
softness = 0
next_chars = []
lines = map(lambda x: x+ ['!'],lines)

sentences_t = []
sentences_t2 = []
sentences_t3 = []
next_chars_t = []
next_chars_t2 = []
next_chars_t3 = []
for line, line_t, line_t2, line_t3 in izip(lines, lines_t, lines_t2, lines_t3):
    for i in range(0, len(line), step):
        if i==0:
            continue
        sentences.append(line[0: i])
        sentences_t.append(line_t[0:i])
        sentences_t2.append(line_t2[0:i])
        sentences_t3.append(line_t3[0:i])
        next_chars.append(line[i])
        if i==len(line)-1: # special case to deal time of end character
            next_chars_t.append(0)
            next_chars_t2.append(0)
            next_chars_t3.append(0)
        else:
            next_chars_t.append(line_t[i])
            next_chars_t2.append(line_t2[i])
            next_chars_t3.append(line_t3[i])
print('nb sequences:', len(sentences))
print('maxlen:', maxlen)

print('Vectorization...')
num_features = len(uniquechars)+4
print('num features: {}'.format(num_features))
X = np.zeros((len(sentences), maxlen, num_features), dtype=np.float32)
y_a = np.zeros((len(sentences), len(target_uchars)), dtype=np.float32)
y_t = np.zeros((len(sentences),3), dtype=np.float32)
for i, sentence in enumerate(sentences):
    leftpad = maxlen-len(sentence)
    next_t = next_chars_t[i]
    next_t2 = next_chars_t2[i]
    next_t3 = next_chars_t3[i]
    sentence_t = sentences_t[i]
    sentence_t2 = sentences_t2[i]
    sentence_t3 = sentences_t3[i]
    for t, char in enumerate(sentence):
        for c in uniquechars:
            if c in char:
                X[i, t+leftpad, uchar_indices[c]] = 1
        X[i, t+leftpad, len(uniquechars)] = t+1
        X[i, t+leftpad, len(uniquechars)+1] = sentence_t[t]/divisor
        X[i, t+leftpad, len(uniquechars)+2] = sentence_t2[t]/divisor2
        X[i, t+leftpad, len(uniquechars)+3] = sentence_t3[t]/divisor3
    for c in target_uchars:
        if c in next_chars[i]:
            y_a[i, target_uchar_indices[c]] = 1-softness
        else:
            y_a[i, target_uchar_indices[c]] = softness/(len(target_uchars)-1)
    y_t[i,0] = next_t/divisor
    y_t[i,1] = next_t2/divisor2
    y_t[i,2] = next_t3/divisor3
    np.set_printoptions(threshold=np.nan)

# output first n batches of matrix
with open("output_files/folds/matrix.txt", "w") as text_file:
    for i in range(0,20):
        for j in range(0,maxlen):
            row = ''
            for k in range(0,num_features):
                row+=str(X[i,j,k])
                row+=','                    
            text_file.write(row+'\n')
        row = ''
        for k in range(0,num_features - 4):
            row+=str(y_a[i,k])
            row+=','
        text_file.write(row+'\n')
        text_file.write('batch end\n')
print('Matrix file has been created...')
            
# build the model: 
print('Build model...')
main_input = Input(shape=(maxlen, num_features), name='main_input')
# train a 2-layer LSTM with one shared layer
l1 = LSTM(par_neurons, consume_less='gpu', init='glorot_uniform', return_sequences=True, dropout_W=par_dropout)(main_input) # the shared layer
b1 = BatchNormalization()(l1)
l2_1 = LSTM(par_neurons, consume_less='gpu', init='glorot_uniform', return_sequences=False, dropout_W=par_dropout)(b1) # the layer specialized in activity prediction
b2_1 = BatchNormalization()(l2_1)
l2_2 = LSTM(par_neurons, consume_less='gpu', init='glorot_uniform', return_sequences=False, dropout_W=par_dropout)(b1) # the layer specialized in time prediction
b2_2 = BatchNormalization()(l2_2)
act_output = Dense(len(target_uchars), activation='softmax', init='glorot_uniform', name='act_output')(b2_1)
time_output = Dense(3, init='glorot_uniform', name='time_output')(b2_2)

model = Model(input=[main_input], output=[act_output, time_output])

if par_algorithm == 1:
    opt = Nadam(lr=0.002, beta_1=0.9, beta_2=0.999, epsilon=1e-08, schedule_decay=0.004, clipvalue=3)
    print('Optimizer: nadam')
elif par_algorithm == 2:
    opt = optimizers.RMSprop(lr=0.001, rho=0.9, epsilon=1e-08, decay=0.0) #"good for rnn"
    print('Optimizer: rmsprop')
elif par_algorithm == 3:
    opt = optimizers.Adamax(lr=0.002, beta_1=0.9, beta_2=0.999, epsilon=1e-08, decay=0.0)
    print('Optimizer: adamax')
elif par_algorithm == 4:
    opt = optimizers.Adam(lr=0.001, beta_1=0.9, beta_2=0.999, epsilon=1e-08, decay=0.0)
    print('Optimizer: adam')
elif par_algorithm == 5:
    opt = optimizers.Adadelta(lr=1.0, rho=0.95, epsilon=1e-08, decay=0.0)
    print('Optimizer: adelta')
elif par_algorithm == 6:
    opt = optimizers.Adagrad(lr=0.01, epsilon=1e-08, decay=0.0)
    print('Optimizer: adagrad')
elif par_algorithm == 7:
    opt = optimizers.SGD(lr=0.01, momentum=0.0, decay=0.0, nesterov=False)
    print('Optimizer: sgd')

model.compile(loss={'act_output':'categorical_crossentropy', 'time_output':'mae'}, optimizer=opt)
early_stopping = EarlyStopping(monitor='val_loss', patience=42)
model_checkpoint = ModelCheckpoint('output_files/models/model-latest.h5', monitor='val_loss', verbose=0, save_best_only=True, save_weights_only=False, mode='auto')
lr_reducer = ReduceLROnPlateau(monitor='val_loss', factor=0.5, patience=par_patience, verbose=0, mode='auto', epsilon=0.0001, cooldown=0, min_lr=0)

model.fit(X, {'act_output':y_a, 'time_output':y_t}, validation_split=0.2, verbose=2, callbacks=[early_stopping, model_checkpoint, lr_reducer], batch_size=maxlen, nb_epoch=500)