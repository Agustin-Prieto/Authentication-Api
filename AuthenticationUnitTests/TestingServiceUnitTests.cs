namespace AuthenticationUnitTests
{
    public class TestingServiceUnitTests
    {
        private readonly IAuthenticationService _authenticationService;

        public TestingServiceUnitTests()
        {
            _authenticationService = Substitute.For<IAuthenticationService>();
        }
        
        
    }
}