        class SharableSpreadSheet
        {
            // The internal design of the object is list of lists. each list is a row that consist of column cells.
            // the mutexes are per row (per list).
            private List<Mutex> rowListMutex; // mutexes for rows
            private List<List<String>> data; // object that stores the data
            private SemaphoreSlim numberOfUsersSemaphore; // semaphore for number of users
            private Mutex fileLoadMutex; // mutex for loading file
            ReaderWriterLockSlim mutexRWLock; // reader-writer locker for mutex list



            public SharableSpreadSheet(int nRows, int nCols, int nUsers = -1)
            {
                // nUsers used for setConcurrentSearchLimit, -1 mean no limit.
                // construct a nRows*nCols spreadsheet
                rowListMutex = new List<Mutex>(); // mutex list for rows
                // initialize mutex list
                for (int i = 0; i < nRows; i++)
                {
                    rowListMutex.Add(new Mutex()); // add new mutex
                }
                data = new List<List<String>>(); // initialize list
                // initialize empty cells in the list
                for (int i = 0; i <  nRows; i++)
                {
                    data.Add(new List<String>());
                    for (int j = 0;  j < nCols; j++) {
                        data[i].Add("");
                    }
                }
                // semaphore for concurrent using
                int maxNumberOfUsers;
                if (nUsers > 0)
                {
                    maxNumberOfUsers = nUsers;
                }
                else
                {
                    maxNumberOfUsers = int.MaxValue;
                }
                numberOfUsersSemaphore = new SemaphoreSlim(maxNumberOfUsers);
                fileLoadMutex = new Mutex(); // mutex for loading file
                mutexRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion); // initialize reader-writer locker for mutex list

            }

            // return the string at [row,col]
            public String getCell(int row, int col)
            {
                // if index out of range, throw exception
                if (row < 0 || row >= data.Count || col < 0 || col >= data[0].Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                String str = ""; // string for return
                mutexRWLock.EnterReadLock(); // read lock mutex list
                rowListMutex[row].WaitOne(); // get lock for row
                str = data.ElementAt(row).ElementAt(col); // get value
                rowListMutex[row].ReleaseMutex(); // release lock for row
                mutexRWLock.ExitReadLock(); // release read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return str; // return string

            }

            // set the string at [row,col]
            public void setCell(int row, int col, String str)
            {
                // if index out of range, throw exception
                if (row < 0 || row >= data.Count || col < 0 || col >= data[0].Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                rowListMutex[row].WaitOne(); // get lock for row
                data[row][col] = str; // change value
                rowListMutex[row].ReleaseMutex(); // release lock for row
                mutexRWLock.ExitReadLock(); // release read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }

            // return first cell indexes that contains the string (search from first row to the last row)
            public Tuple<int, int> searchString(String str)
            {
                int colIndex = -1; // index of col of cell
                int rowIndex = -1; // index of row of cell
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                for (int i = 0; i < data.Count; i++)
                {
                    rowListMutex[i].WaitOne(); // lock row
                    for (int j = 0; j < data[0].Count; j++)
                    {
                        String strFromCell = data[i][j]; // get string from cell
                        if (strFromCell.Equals(str)) // if string equals to str
                        {
                            rowIndex = i;
                            colIndex = j;
                        }
                    }
                    rowListMutex[i].ReleaseMutex(); // release mutex
                }
                mutexRWLock.ExitReadLock(); // exit read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return new Tuple<int, int>(rowIndex, colIndex); // return index of cell
            }

            // exchange the content of row1 and row2
            public void exchangeRows(int row1, int row2)
            {
                // if index out of range, break
                if (row1 < 0 || row1 >= data.Count || row2 < 0 || row2 >= data.Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                int minRow = Math.Min(row1, row2); // get min row
                int maxRow = Math.Max(row1, row2); // get max row
                mutexRWLock.EnterReadLock(); // read lock mutex list
                // lock min row first and then max row, so we won't get deadlock
                rowListMutex[minRow].WaitOne(); // get lock for min row
                rowListMutex[maxRow].WaitOne(); // get lock for max row
                // exchange rows
                List<String> temp = data.ElementAt(minRow);
                data[minRow] = data[maxRow];
                data[maxRow] = temp;
                rowListMutex[maxRow].ReleaseMutex(); // release lock for max row
                rowListMutex[minRow].ReleaseMutex(); // release lock for min row
                mutexRWLock.ExitReadLock(); // exit read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }

            // exchange the content of col1 and col2
            public void exchangeCols(int col1, int col2)
            {
                // if index out of range, throw exception
                if (col1 < 0 || col1 >= data[0].Count || col2 < 0 || col2 >= data[0].Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                // iterate all rows, and exchange values of two columns in each row
                for (int i = 0; i < data.Count; i++)
                {
                    rowListMutex[i].WaitOne(); // lock row
                    // exchange rows
                    String temp = data[i][col1];
                    data[i][col1] = data[i][col2];
                    data[i][col2] = temp;
                    rowListMutex[i].ReleaseMutex(); // release lock
                }
                mutexRWLock.ExitReadLock(); // exit read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }

            // perform search in specific row
            public int searchInRow(int row, String str)
            {
                // if index out of range, throw exception
                if (row < 0 || row >= data.Count)
                    throw new IndexOutOfRangeException();
                int index = -1; // index of cell
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                rowListMutex[row].WaitOne(); // lock row that is being searched

                for (int i = 0; i < data[0].Count; i++)
                {
                    // if found, return it
                    if (data[row][i] == str)
                    {
                        index = i;
                    }
                }
                rowListMutex[row].ReleaseMutex(); // release row mutex
                mutexRWLock.ExitReadLock(); // exit read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return index; // if not found, return -1

            }

            // perform search in specific col
            public int searchInCol(int col, String str)
            {
                int index = -1; // index of cell
                // if index out of range, throw exception
                if (col < 0 || col >= data[0].Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                for (int i = 0; i < data.Count; i++)
                {
                    rowListMutex[i].WaitOne(); // lock
                    String strFromCell = getCell(i, col); // get string from cell
                    if (strFromCell.Equals(str)) // if string equals to str
                    {
                        index = i; // set index of cell
                    }
                    rowListMutex[i].ReleaseMutex(); // release
                    if (index != -1)
                        break; // if found, break
                }
                mutexRWLock.ExitReadLock(); // read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return index; // if not found, return -1
            }

            // perform search within spesific range: [row1:row2,col1:col2] 
            //includes col1,col2,row1,row2
            public Tuple<int, int> searchInRange(int col1, int col2, int row1, int row2, String str)
            {
                int colIndex = -1; // index of col of cell
                int rowIndex = -1; // index of row of cell
                // if index out of range, throw exception
                if (row1 < 0 || row1 >= data.Count || row2 < 0 || row2 >= data.Count || col1 < 0 || col1 >= data[0].Count || col2 < 0 || col2 >= data[0].Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                for (int i = row1; i < row2; i++) {
                    rowListMutex[i].WaitOne(); // lock row
                    for (int j = col1; j < col2; j++)
                    {
                        String strFromCell = data[i][j]; // get string from cell
                        if (strFromCell.Equals(str)) // if string equals to str
                        {
                            rowIndex = i;
                            colIndex = j;
                        }
                    }
                    rowListMutex[i].ReleaseMutex(); // release mutex
                    if (colIndex != -1 || rowIndex != -1)
                        break; // if found, break
                }
                mutexRWLock.ExitReadLock(); // exit read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return new Tuple<int, int>(rowIndex, colIndex); // return index of cell
            }

            //add a row after row1
            public void addRow(int row1)
            {
                // if index out of range, throw exception
                if (row1 < 0 || row1 >= data.Count)
                    throw new IndexOutOfRangeException();
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterWriteLock(); // enter write lock mutex list
                // lock all rows
                for (int i =0; i < rowListMutex.Count; i++)
                {
                    rowListMutex[i].WaitOne();
                }
                // create new list, in size of columns
                List<String> newRow = new List<String>();
                // initialize with empty strings
                for (int i = 0; i < data[0].Count; i++)
                {
                    newRow.Add("");
                }
                // add new row to list
                data.Insert(row1 + 1, newRow);
                // add new mutex to list of mutexes
                Mutex newMutex = new Mutex(); // lock the new mutex
                newMutex.WaitOne(); // lock new mutex
                rowListMutex.Insert(row1 + 1, newMutex);
                // release all locks
                for (int i = 0; i < rowListMutex.Count; i++)
                {
                     rowListMutex[i].ReleaseMutex();
                }
                mutexRWLock.ExitWriteLock(); // exit write lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }

            //add a column after col1
            public void addCol(int col1)
            {
                // if index out of range, throw exception
                if (col1 < 0 || col1 >= data[0].Count)
                    throw new IndexOutOfRangeException();

                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                // iterate all rows
                for (int i = 0; i < data.Count; i++)
                {
                    // lock row
                    rowListMutex[i].WaitOne();
                    // insert new element in required index
                    data[i].Insert(col1+1, "");
                    // release mutex
                    rowListMutex[i].ReleaseMutex();
                }
                mutexRWLock.ExitReadLock(); // read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users

            }

            // perform search and return all relevant cells according to caseSensitive param
            public Tuple<int, int>[] findAll(String str, bool caseSensitive)
            {
                List<Tuple<int, int>> arrayOfIndexes = new List<Tuple<int, int>>(); // create empty list of cells    
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                // iterate data
                for (int i = 0; i < data.Count; i++)
                {
                    // lock row
                    rowListMutex[i].WaitOne();
                    for (int j = 0; j < data[0].Count; j++)
                    {
                        String strFromCell = data[i][j]; // get string from cell
                        // if cell contains string (case sensitive or not, based on user choosing)
                        if ((caseSensitive && str.Equals(strFromCell, StringComparison.Ordinal)) || (!caseSensitive && str.Equals(strFromCell, StringComparison.OrdinalIgnoreCase)))
                        {
                            arrayOfIndexes.Add(new Tuple<int, int>(i, j)); // add cell to list
                        }
                    }
                    // release mutex
                    rowListMutex[i].ReleaseMutex();
                }
                mutexRWLock.ExitReadLock(); // exit read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return arrayOfIndexes.ToArray(); // return array of cells
            }

            // replace all oldStr cells with the newStr str according to caseSensitive param
            public void setAll(String oldStr, String newStr, bool caseSensitive)
            {
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                Tuple<int, int>[] arrayOfIndexes = findAll(oldStr, caseSensitive); // get all cells with oldStr
                for (int i = 0; i < arrayOfIndexes.Length; i++) // iterate all cells
                {
                    setCell(arrayOfIndexes.ElementAt(i).Item1, arrayOfIndexes.ElementAt(i).Item2, newStr); // replace cell value
                }
                mutexRWLock.ExitReadLock(); // read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }

            // return the size of the spreadsheet in nRows, nCols
            public Tuple<int, int> getSize()
            {
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                mutexRWLock.EnterReadLock(); // read lock mutex list
                int rows = data.Count; // save number of rows
                int cols = data[0].Count; // save number of columns
                mutexRWLock.ExitReadLock(); // read lock mutex list
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
                return new Tuple<int, int>(rows, cols);

            }

            // save the spreadsheet to a file fileName.
            // you can decide the format you save the data. There are several options.
            public void save(String fileName)
            {
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                try
                {
                    fileLoadMutex.WaitOne();
                    FileStream fileStream = new FileStream(fileName, FileMode.Create);
                    // Create a binary formatter for serialization
                    BinaryFormatter formatter = new BinaryFormatter();
                    // lock current object, so no other thread access it while manipulation
                    for (int i = 0; i < rowListMutex.Count; i++)
                    {
                        rowListMutex[i].WaitOne();
                    }
                    // Serialize and write the data and mutex list
                    formatter.Serialize(fileStream, data);
                    fileStream.Close(); // close filestream

                    // unlock all mutexes
                    for (int i = 0; i < rowListMutex.Count; i++)
                    {
                        rowListMutex[i].ReleaseMutex();
                    }

                } catch (Exception e)
                {
                    Console.WriteLine("Error has occurred: " + e.Message);
                }
                finally
                {
                    fileLoadMutex.ReleaseMutex();
                }
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }

            // load the spreadsheet from fileName
            // replace the data and size of the current spreadsheet with the loaded data
            public void load(String fileName)
            {
                numberOfUsersSemaphore.Wait(); // lock semaphore for number of users
                // if file doesn't exist, throw error message
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("Can't read file that doesn't exist!");
                }
                else
                {
                    try
                    {
                        fileLoadMutex.WaitOne();
                        FileStream fileStream = new FileStream(fileName, FileMode.Open);
                        // Create a binary formatter for deserialization
                        BinaryFormatter formatter = new BinaryFormatter();
                        // lock current object, so no other thread access it while manipulation
                        for (int i = 0; i < rowListMutex.Count; i++)
                        {
                            rowListMutex[i].WaitOne();
                        }
                        // deserialize and read the data
                        data = (List<List<String>>)formatter.Deserialize(fileStream);
                        fileStream.Close(); // close filestream
                        // add locked mutexes if necessary
                        while (rowListMutex.Count < data.Count)
                        {
                            Mutex newMutex = new Mutex(); // create new mutex
                            newMutex.WaitOne(); // lock it so other threads won't access it until process is done
                            rowListMutex.Add(newMutex); // add it to list

                        }
                        // unlock all mutexes
                        for (int i = 0; i < rowListMutex.Count; i++)
                        {
                            rowListMutex[i].ReleaseMutex();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error has occurred: " + e.Message);
                    }
                    finally
                    {
                        fileLoadMutex.ReleaseMutex();
                    }
                }
                numberOfUsersSemaphore.Release(); // release semaphore for number of users
            }
        }