lines = open("Book1.csv", "r").readlines()
headings = [heading.strip() for heading in lines[0].split("\t")]
outf = open("Formatted.sql", "w")
outf.write("insert into TransactionRecord (" + ", ".join(headings) + ") values\n");
outlines = []

def quote(s):
	return "'%s'"%s if s.lower() != "null" else s
		
for line in lines[1:]:
	fields = [field.strip() for field in line.split("\t")]
	fields[3]  = quote(fields[ 3])
	fields[8]  = quote(fields[ 8])
	fields[9]  = quote(fields[ 9])
	fields[13] = quote(fields[13])
	fields[14] = quote(fields[14])
	fields[15] = quote(fields[15])
	fields[18] = quote(fields[18])
	fields[19] = quote(fields[19])
	fields[25] = quote(fields[25])
	outlines.append("(%s)" % ", ".join(fields))
	
outf.write(",\n".join(outlines))
	