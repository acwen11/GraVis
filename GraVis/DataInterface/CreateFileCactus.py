# use script to read 3D hdf-files
import numpy as np

from kuibit.simdir import SimDir
import numpy
import struct
import time
#source_path = "data/" # "E:/SimulationData/data/"
datadir = "/lagoon/allenwen/LDP_LR_noleak" # "E:/SimulationData/data/"
pickle_file = "/lagoon/allenwen/LDP_LR_noleak.pickle" # "E:/SimulationData/data/"
shape = [256, 256, 256]

# The code can currently only handle data in a 256^3 point grid. Set the corners of the cube here.
x0 = [-24, -24, -24]
x1 = [24, 24, 24]


total = 124 # Number of 3D snapshots. 1798 is the max

# Enable to generate scalar or vector output. Rho isosurface rendering is currently broken but the code requires these files to exist anyway.
transformVectorData = True 
transformRhoData = True


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
#	 # use < for little endian and > for big endian
#	 structureData.write(bytearray(struct.pack("<%ui" % len(data["MeshBlockSize"]), * data["MeshBlockSize"])))
#	 structureData.write(bytearray(struct.pack("<%ui" % len(data["RootGridSize"]), *data["RootGridSize"])))
#	 structureData.write(bytearray(struct.pack("<%ui" % len(data["Levels"]), *data["Levels"])))#
#
#	 coordList = np.array(list(zip(data["x1v"], data["x2v"], data["x3v"]))).ravel()
#	 structureData.write(bytearray(struct.pack("<%uf" % len(coordList), *coordList)))

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
	print("Reading Vector Data")
	with SimDir(
		datadir,
		pickle_file=pickle_file,
	) as sim:
		reader = sim.gridfunctions['xyz']
		rho = reader["rho_b"]
		print("Read rho")

	rangelist = list(range(0,total))
	for i in rangelist:
		print("Reading output {:d} of {:d}.\n".format(i, total))
		iteration = i * 8192
		start_time_sample = time.time()

		rhodata = rho[iteration].to_UniformGridData(shape, x0, x1).data.flatten()

		dataList = rhodata.ravel()

		with open('otdata/Rho/Rho' + getIndexString(i) + '_MIP0.otdata', 'wb') as rhoData:
			rhoData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))

		timeTaken = time.time() - start_time_sample
		totalTime = time.time() - start_time_total
		timeLeft = totalTime / (i + 1.0) * (total - (i + 1.0))
		print("Data completed in " + time_convert(timeTaken))
		print("Total time: " + time_convert(totalTime) + ". Time left: " + time_convert(timeLeft))
		print("Datasets done by " + str((i + 1) / (total) * 100) + "%")
		rho.clear_cache()


if (transformVectorData):
	print("Reading Vector Data")
	with SimDir(
		datadir,
		pickle_file=pickle_file,
	) as sim:
		reader = sim.gridfunctions['xyz']
		Bx = reader["Bx"]
		By = reader["By"]
		Bz = reader["Bz"]
		print("Read Bi")

	rangelist = list(range(0,total))
	for i in rangelist:
		print("Reading output {:d} of {:d}.\n".format(i, total))
		iteration = i * 8192
		start_time_sample = time.time()

		Bxdata = Bx[iteration].to_UniformGridData(shape, x0, x1).data.flatten()
		Bydata = By[iteration].to_UniformGridData(shape, x0, x1).data.flatten()
		Bzdata = Bz[iteration].to_UniformGridData(shape, x0, x1).data.flatten()

		values = np.empty((3, 256*256*256))
		values[0] = Bxdata
		values[1] = Bydata
		values[2] = Bzdata
		values = values.T.flatten() # convert to 1D [ Bx(0,0,0), By(0,0,0), Bz(0,0,0), ... ]
		dataList = values.ravel()

		with open('otdata/B/B' + getIndexString(i) + '_MIP0.otdata', 'wb') as vectorData:
			vectorData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))

		timeTaken = time.time() - start_time_sample
		totalTime = time.time() - start_time_total
		timeLeft = totalTime / (i+1.0) * (total-(i+1.0))
		print("Data completed in "+time_convert(timeTaken))
		print("Total time: " + time_convert(totalTime)+". Time left: " + time_convert(timeLeft))
		print("Datasets done by " + str((i+1) / (total) * 100) + "%")
		Bx.clear_cache()
		By.clear_cache()
		Bz.clear_cache()
