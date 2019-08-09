using System.Threading.Tasks;
using GraphQL.Utilities.Federation;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class FederatedSchemaBuilderTestBase : SchemaBuilderTestBase
    {
        public FederatedSchemaBuilderTestBase()
        {
            Builder = new FederatedSchemaBuilder();
        }
    }

    public class FederatedSchemaBuilderTests : FederatedSchemaBuilderTestBase
    {
        public class User
        {
            public string Id { get; set; }
            public string Username { get; set; }
        }

        [Fact]
        public void returns_sdl()
        {
            var definitions = @"
                extend type Query {
                    me: User
                }

                type User @key(fields: ""id"") {
                    id: ID! @external
                    username: String!
                }
            ";

            var schema = FederatedSchema.For(definitions);

            var result = schema.Execute(_ => {
                _.Query = "{ _service { sdl } }";
            });

            result.ShouldNotBeNull();
        }

        [Fact]
        public void entity_query()
        {
            var definitions = @"
                extend type Query {
                    me: User
                }

                type User @key(fields: ""id"") {
                    id: ID!
                    username: String!
                }
            ";

            Builder.Types.For("User").ResolveReferenceAsync(ctx =>
            {
                return Task.FromResult(new User { Id = "123", Username = "Quinn" });
            });

            var query = @"
                query ($_representations: [_Any!]!) {
                    _entities(representations: $_representations) {
                        ... on User {
                            id
                            username
                        }
                    }
                }";

            var variables = "{ '_representations': [{ '__typename': 'User', 'id': '123' }] }";
            var expected = @"{ '_entities': [{ '__typename': 'User', 'id' : '123', 'username': 'Quinn' }] }";

            AssertQuery(_ =>
            {
                _.Definitions = definitions;
                _.Query = query;
                _.Variables = variables;
                _.ExpectedResult = expected;
            });
        }
    }
}