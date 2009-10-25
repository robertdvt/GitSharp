﻿/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GitSharp.Core.DirectoryCache;
using System.Diagnostics;

namespace Git
{

    /// <summary>
    /// Represents the index of a git repository which keeps track of changes that are about to be committed.
    /// </summary>
    public class Index : IDisposable
    {
        private Repository _repo;
        private DirCache _index;

        public Index(Repository repo)
        {
            if (repo.IsBare)
                throw new ArgumentException("Bare repositories have not got an index.");
            _repo = repo;
            _index = DirCache.read(_repo._internal_repo);
        }

        /// <summary>
        /// Add all untracked files to the index and writes the index to the disk (like "git add .")
        /// </summary>
        public void AddAll()
        {
            Add(_repo.WorkingDirectory);
        }

        /// <summary>
        /// Adds untracked files or directories to the index and writes the index to the disk (like "git add")
        /// 
        /// Note: Add as many files as possible by one call of this method for best performance.
        /// </summary>
        /// <param name="paths">Paths to add to the index</param>
        public void Add(params string[] paths)
        {
            try
            {
                _index.Lock();
                var builder = _index.builder();
                foreach (var path in paths)
                {
                    if (new FileInfo(path).Exists)
                        AddFile(new FileInfo(path), builder);
                    else if (new DirectoryInfo(path).Exists)
                        AddDirectory(new DirectoryInfo(path), builder);
                    else
                        throw new ArgumentException("File or directory at <" + path + "> doesn't seem to exist.", "path");
                }
                builder.finish();
                _index.write();
                _index.commit(); // save index, does not create a commit object.
            }
            finally
            {
                _index.unlock();
            }
        }

        private void AddFile(FileInfo path, DirCacheBuilder builder)
        {
            builder.add(new DirCacheEntry(GitSharp.Core.Constants.encode(path.FullName)));
            //GitIndex.add(_repo._internal_repo.WorkingDirectory, new FileInfo(path));
        }

        private void AddDirectory(DirectoryInfo path, DirCacheBuilder builder)
        {
            AddRecursively(path, builder);
        }

        private void AddRecursively(DirectoryInfo dir, DirCacheBuilder builder)
        {
            foreach (var file in dir.GetFiles())
                AddFile(file, builder);
            foreach (var subdir in dir.GetDirectories())
                AddDirectory(subdir, builder);
        }

        /// <summary>
        /// Writes the index to the disk.
        /// </summary>
        public void Write()
        {
            try
            {
                _index.Lock();
                _index.write();
                _index.commit(); // save index, does not create a commit object.
            }
            finally
            {
                _index.unlock();
            }
        }

        /// <summary>
        /// Reads the index from the disk
        /// </summary>
        public void Read()
        {
            _index.read();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_index == null)
                return;
            _index.unlock();
            _index = null;
            _repo = null;
        }

        #endregion
    }
}
