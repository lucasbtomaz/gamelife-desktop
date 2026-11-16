import LibraryGameForm from '../components/LibraryGameForm.jsx';

export default function LibraryGameScreen({ game, platforms, saving, onSubmit, onBack, message }) {
  const editing = Boolean(game);

  return <main id="biblioteca-formulario" className="library-game-screen">
    <button type="button" className="library-back-button" onClick={onBack}>Voltar para a biblioteca</button>

    <header className="page-header">
      <div>
        <span className="eyebrow">{editing ? 'EDITAR FICHA' : 'NOVA FICHA'}</span>
        <h1>{editing ? `Editar ${game.titulo}` : 'Adicionar jogo'}</h1>
        <p>{editing
          ? 'Atualize as plataformas e as informações opcionais deste jogo.'
          : 'Cadastre o jogo uma única vez e marque todas as plataformas onde você o possui.'}</p>
      </div>
    </header>

    <section className="form-card library-editor-card">
      <LibraryGameForm
        key={game?.jogoId ?? 'novo'}
        platforms={platforms}
        saving={saving}
        onSubmit={onSubmit}
        initialGame={game}
        onCancel={onBack}
      />
      {message && <div className={`feedback ${message.tipo}`} role="alert">{message.texto}</div>}
    </section>
  </main>;
}
