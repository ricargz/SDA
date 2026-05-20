// A03 Injection - Login con SQL

// Vulnerable: concatena datos del usuario directamente en SQL
const sql = "SELECT * FROM usuarios WHERE correo = '" + correo + "' AND password = '" + password + "'";
const usuario = db.query(sql);

// Seguro: usa consulta parametrizada y contraseña con hash
const sql = "SELECT * FROM usuarios WHERE correo = ?";
const usuario = db.query(sql, [correo]);
const valido = await bcrypt.compare(password, usuario.password_hash);



//A01 Broken Access Control - Consulta de perfil

// Vulnerable: cualquier usuario autenticado puede consultar cualquier id
app.get('/api/usuarios/:id', auth, async (req, res) => {
  const usuario = await Usuario.findById(req.params.id);
  res.json(usuario);
});

// Seguro: valida propiedad del recurso o rol administrador
app.get('/api/usuarios/:id', auth, async (req, res) => {
  const esPropietario = req.user.id === req.params.id;
  const esAdmin = req.user.rol === 'admin';

  if (!esPropietario && !esAdmin) {
    return res.status(403).json({ error: 'Acceso denegado' });
  }

  const usuario = await Usuario.findById(req.params.id);
  res.json(usuario);
});



//A02 Cryptographic Failures - Contraseñas

// Vulnerable: usa hash débil y sin salt
const hash = sha1(password);
await Usuario.create({ correo, password_hash: hash });

// Seguro: usa algoritmo para contraseñas con salt y costo
const hash = await bcrypt.hash(password, 12);
await Usuario.create({ correo, password_hash: hash });
