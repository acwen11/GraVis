
# use athena_read script to read the hdf-files
import numpy as np

import athena_read
import numpy
import random
import struct
import time
source_path = "data/" # "E:/SimulationData/data/"
source_file = "gr_mhd_tov.out1.00000.athdf"


total = 50 #1798 is the max amount of data sets
transformVectorData = False
transformRhoData = False


with open('metaData' + '.meta', 'wb') as metaData:
    metaData.write(bytearray(struct.pack("<i", 0))) # enum type
    metaData.write(bytearray(struct.pack("<i", 4)))  # how many variables: rho, press, velocity, B
    name = "Rho".encode('utf-8')
    metaData.write(bytearray(struct.pack("<i", len(name)))) #size of string
    metaData.write(bytearray(struct.pack("@%us" %len(name),name)))
    metaData.write(bytearray(struct.pack("<iiii", *[256,256,256,1]))) #dimension
    name = "press".encode('utf-8')
    metaData.write(bytearray(struct.pack("<i", len(name))))  # size of string
    metaData.write(bytearray(struct.pack("@%us" % len(name), name)))
    metaData.write(bytearray(struct.pack("<iiii", *[256, 256, 256, 1])))  # dimension
    name = "velocity".encode('utf-8')
    metaData.write(bytearray(struct.pack("<i", len(name))))  # size of string
    metaData.write(bytearray(struct.pack("@%us" % len(name), name)))
    metaData.write(bytearray(struct.pack("<iiii", *[256, 256, 256, 3])))  # dimension
    name = "B".encode('utf-8')
    metaData.write(bytearray(struct.pack("<i", len(name))))  # size of string
    metaData.write(bytearray(struct.pack("@%us" % len(name), name)))
    metaData.write(bytearray(struct.pack("<iiii", *[256, 256, 256, 3])))  # dimension



#print(data)
# Create a file for each data set to be able to load datasets dynamically
# To reduce redundancy, create a structure file for the datastructure and coordinate values

# structure of the meta data set:
# - HEADER int, int, int, int, int, int: 24 bytes:
# blockSizeX, blockSizeY, blockSizeZ, blockDimX, blockDimY, blockDimZ
# - n x Level: n * int
# - n blocks: n x (blockSizeX + blockSizeY + blockSizeZ) x 12 bytes:
# blockSizeX * XCoord
# blockSizeY * YCoord
# blockSizeZ * ZCoord

# structure of a data set:
# - n blocks: n x blockSizeX x blockSizeY x blockSizeZ x (3 or 1) x 4 bytes:
# (0 ... blockSizeX, 0 ... blockSizeY, 0 ... blockSizeZ): ValueX, ValueY, ValueZ
# with: (0...blockSizeX,0,0) ... (0...blockSizeX, blockSizeY,0) ... (0...blockSizeX, 0...blockSizeY,blockSizeZ)

# the level is kept on a stack (with link to the actual block) to maintain the child-parent hierarchy
# use IEEE 754 standard to save floats.




# Structure Data
#with open('structureData' + str(data["Time"]) + '.otdata', 'wb') as structureData:
#    # use < for little endian and > for big endian
#    structureData.write(bytearray(struct.pack("<%ui" % len(data["MeshBlockSize"]), * data["MeshBlockSize"])))
#    structureData.write(bytearray(struct.pack("<%ui" % len(data["RootGridSize"]), *data["RootGridSize"])))
#    structureData.write(bytearray(struct.pack("<%ui" % len(data["Levels"]), *data["Levels"])))#
#
#    coordList = np.array(list(zip(data["x1v"], data["x2v"], data["x3v"]))).ravel()
#    structureData.write(bytearray(struct.pack("<%uf" % len(coordList), *coordList)))

def getIndexString(index, digits = 5):
    indexString = str(index)
    if len(indexString) > digits:
        digits = len(indexString)
    return "0"*(digits - len(indexString)) + indexString

def time_convert(sec):
  mins = sec // 60
  sec = sec % 60
  hours = mins // 60
  mins = mins % 60
  return getIndexString(int(hours),2) +":"+ getIndexString(int(mins),2)+":" + getIndexString(int(sec),2) + "(h:m:s)"

start_time_total = time.time()

if (transformRhoData):
    rangelist = list(range(0, total))
    for i in rangelist:
        start_time_sample = time.time()
        data = athena_read.athdf(source_path + "gr_mhd_tov.out1." + getIndexString(i) + ".athdf", raw=True)
        values = [0.0] * 256 * 256 * 256
        with open('otdata/Rho/Rho' + getIndexString(i) + '.otdata', 'wb') as rhoData:
            for blockID in range(0, 4096):
                for x in range(0, 16):
                    for y in range(0, 16):
                        for z in range(0, 16):
                            value = data["rho"][blockID][x][y][z]
                            level = data["LogicalLocations"][blockID]

                            _x = level[0] * 16 + z
                            _y = level[1] * 16 + y
                            _z = level[2] * 16 + x
                            index = _x + 256 * _y + 256 * 256 * _z
                            values[index] = value
                # for first row: x, so + 0
                # for second row: y, so + 1
                # for third row: z, so + 2

            dataList = np.array(values).ravel()
            rhoData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))

        timeTaken = time.time() - start_time_sample
        totalTime = time.time() - start_time_total
        timeLeft = totalTime / (i + 1.0) * (total - (i + 1.0))
        print("Data completed in " + time_convert(timeTaken))
        print("Total time: " + time_convert(totalTime) + ". Time left: " + time_convert(timeLeft))
        print("Datasets done by " + str((i + 1) / (total) * 100) + "%")


if (transformVectorData):
    rangelist = list(range(0,1798))
    for i in rangelist:
        start_time_sample = time.time()
        data = athena_read.athdf(source_path + "gr_mhd_tov.out1."+getIndexString(i)+".athdf", raw=True)
        values = [0.0] * 256*256*256*3
        with open('otdata/B/B' + getIndexString(i) + '.otdata', 'wb') as vectorData:
            for blockID in range(0, 4096):
                for x in range(0, 16):
                    for y in range(0, 16):
                        for z in range(0, 16):
                            level = data["LogicalLocations"][blockID]

                            _x = level[0] * 16 + z
                            _y = level[1] * 16 + y
                            _z = level[2] * 16 + x
                            index = _x + 256 * _y + 256 * 256 * _z
                            values[index * 3] = data["Bcc1"][blockID][x][y][z]
                            values[index * 3 + 1] = data["Bcc2"][blockID][x][y][z]
                            values[index * 3 + 2] = data["Bcc3"][blockID][x][y][z]
                #if blockID%256 == 0:
                #    print("Dataset" + str(i) + ": done by " + str(blockID/4096.0 * 100) + "%")
                # for first row: x, so + 0
                # for second row: y, so + 1
                # for third row: z, so + 2
            #print("Done reading.")
            dataList = np.array(values).ravel()

            vectorData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))
            #print("Done writing.")
        timeTaken = time.time() - start_time_sample
        totalTime = time.time() - start_time_total
        timeLeft = totalTime / (i+1.0) * (total-(i+1.0))
        print("Data completed in "+time_convert(timeTaken))
        print("Total time: " + time_convert(totalTime)+". Time left: " + time_convert(timeLeft))
        print("Datasets done by " + str((i+1) / (total) * 100) + "%")
# single data sets
#with open('rhoData' + str(data["Time"]) + '.txt', 'wb') as rhoData:
#    rhoData.write(bytearray(struct.pack("<i", 1)))
#    i = 0
#    for block in data["rho"]:
#        for zDim in block:
#                dataList = np.array(zDim).ravel()
#                rhoData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))


#for dataset_index, dataset_name in enumerate(data['DatasetNames']):

