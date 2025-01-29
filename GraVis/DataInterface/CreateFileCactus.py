
# use athena_read script to read the hdf-files
import numpy as np

# import athena_read
from kuibit.simdir import SimDir
import numpy
import struct
import time
#source_path = "data/" # "E:/SimulationData/data/"
datadir = "/lagoon/allenwen/LDP_LR_noleak" # "E:/SimulationData/data/"
pickle_file = "/lagoon/allenwen/LDP_LR_noleak.pickle" # "E:/SimulationData/data/"
shape = [256, 256, 256]
x0 = [-24, -24, -24]
x1 = [24, 24, 24]


total = 124 #1798 is the max amount of data sets
transformVectorData = True 
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
		#data = athena_read.athdf(source_path + "gr_mhd_tov.out1."+getIndexString(i)+".athdf", raw=True)

		Bxdata = Bx[iteration].to_UniformGridData(shape, x0, x1).data.flatten()
		Bydata = By[iteration].to_UniformGridData(shape, x0, x1).data.flatten()
		Bzdata = Bz[iteration].to_UniformGridData(shape, x0, x1).data.flatten()

		# values = [0.0] * 256*256*256*3
		values = np.empty((3, 256*256*256))
		values[0] = Bxdata
		values[1] = Bydata
		values[2] = Bzdata
		values = values.T.flatten() # convert to 1D [ Bx(0,0,0), By(0,0,0), Bz(0,0,0), ... ]
		datalist = values.ravel()

		with open('otdata/B/B' + getIndexString(i) + '.otdata', 'wb') as vectorData:
			vectorData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))

		# Athena stuff vvvvv
		#with open('otdata/B/B' + getIndexString(i) + '.otdata', 'wb') as vectorData:
		# for blockID in range(0, 4096):
		#	  for x in range(0, 16):
		#		  for y in range(0, 16):
		#			  for z in range(0, 16):
		#				  _x = level[0] * 16 + z
		#				  _y = level[1] * 16 + y
		#				  _z = level[2] * 16 + x
		#				  index = _x + 256 * _y + 256 * 256 * _z
		#				  values[index * 3] = data["Bcc1"][blockID][x][y][z]
		#				  values[index * 3 + 1] = data["Bcc2"][blockID][x][y][z]
		#				  values[index * 3 + 2] = data["Bcc3"][blockID][x][y][z]
		#	  #if blockID%256 == 0:
		#	  #    print("Dataset" + str(i) + ": done by " + str(blockID/4096.0 * 100) + "%")
		#	  # for first row: x, so + 0
		#	  # for second row: y, so + 1
		#	  # for third row: z, so + 2
		#print("Done reading.")
		#dataList = np.array(values).ravel()
		# Athena stuff ^^^^^

		# for kk in range(16):
		#	for jj in range(16):
		#	  for ii in range(16):
		#		imin = ii * 16
		#		imax = (ii + 1) * 16
		#		jmin = jj * 16
		#		jmax = (jj + 1) * 16
		#		kmin = kk * 16
		#		kmax = (kk + 1) * 16

		#		chdatax = Bxdata[imin:imax, jmin:jmax, kmin:kmax]
		#		chdatay = Bydata[imin:imax, jmin:jmax, kmin:kmax]
		#		chdataz = Bzdata[imin:imax, jmin:jmax, kmin:kmax]

		#		chdata_vec = np.empty((3, len(chdatax))
		#		chdata_vec[0] = chdatax
		#		chdata_vec[1] = chdatay
		#		chdata_vec[2] = chdataz
		#		chdata_vec = chdata_vec.T.flatten() # convert to 1D [ Bx(0,0,0), By(0,0,0), Bz(0,0,0), ... ]
		#		outname = args.dataout + "_{:d}{:d}{:d}.txt".format(ii, jj, kk)
		#		output_path = os.path.join(args.dataout, outname)

		#		print("Saving to {:s}".format(output_path))
		#		# TODO: Save as binary
		#		np.savetxt(output_path, chdata)


		#print("Done writing.")
		timeTaken = time.time() - start_time_sample
		totalTime = time.time() - start_time_total
		timeLeft = totalTime / (i+1.0) * (total-(i+1.0))
		print("Data completed in "+time_convert(timeTaken))
		print("Total time: " + time_convert(totalTime)+". Time left: " + time_convert(timeLeft))
		print("Datasets done by " + str((i+1) / (total) * 100) + "%")
# single data sets
#with open('rhoData' + str(data["Time"]) + '.txt', 'wb') as rhoData:
#	 rhoData.write(bytearray(struct.pack("<i", 1)))
#	 i = 0
#	 for block in data["rho"]:
#		 for zDim in block:
#				 dataList = np.array(zDim).ravel()
#				 rhoData.write(bytearray(struct.pack("<%uf" % len(dataList), *dataList)))


#for dataset_index, dataset_name in enumerate(data['DatasetNames']):

