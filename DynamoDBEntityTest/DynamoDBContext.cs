using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

using System.Text;

namespace DynamoDBEntityTest {
    public class DynamoDBContext : DbContext {
		public DynamoDBContext() {

		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			
		}
    }
}
